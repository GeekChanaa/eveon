using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Billing
{
    /// <summary>
    /// Hooks of the charging and payment flows into invoicing. They never throw: a document that cannot be issued
    /// now is issued by <see cref="InvoiceSweeper"/> or on first download, and the flow that moved the money goes on.
    /// </summary>
    public interface IBillingEvents
    {
        /// <summary>A session was billed or its idle fee settled: invoices it once the car left.</summary>
        Task SessionBilledAsync(int sessionId, CancellationToken ct = default);

        /// <summary>A top-up was credited: invoices it and emails the invoice to the customer.</summary>
        Task TopUpCompletedAsync(int orderId, CancellationToken ct = default);
    }

    public sealed class BillingEvents : IBillingEvents
    {
        private readonly IInvoiceService _invoices;
        private readonly BillingMailer _mailer;
        private readonly VoltaXApiDbContext _db;
        private readonly ILogger<BillingEvents> _logger;

        public BillingEvents(IInvoiceService invoices, BillingMailer mailer, VoltaXApiDbContext db, ILogger<BillingEvents> logger)
        {
            _invoices = invoices;
            _mailer = mailer;
            _db = db;
            _logger = logger;
        }

        public async Task SessionBilledAsync(int sessionId, CancellationToken ct = default)
        {
            try
            {
                // While the car is still plugged in the idle fee is unknown: wait for it (the sweeper catches
                // sessions whose charger never reports the connector free).
                var completed = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                    .AnyAsync(s => s.ID == sessionId && s.ChargingSessionStatus == ChargingSessionStatusEnum.Completed, ct);
                if (completed)
                    await _invoices.IssueForSessionAsync(sessionId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The invoice of session {SessionId} could not be issued now; the sweeper will retry", sessionId);
            }
        }

        public async Task TopUpCompletedAsync(int orderId, CancellationToken ct = default)
        {
            try
            {
                var invoice = await _invoices.IssueForOrderAsync(orderId, ct);
                if (invoice == null)
                    return;
                var amount = BillingTexts.Money(invoice.AmountInclVat, invoice.Language);
                await _mailer.SendAsync(invoice,
                    BillingTexts.Format(invoice.Language, "MailTopUpSubject", amount, invoice.Number),
                    BillingTexts.Format(invoice.Language, "MailTopUpBody", amount, invoice.Number), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The invoice of top-up {OrderId} could not be issued now; the sweeper will retry", orderId);
            }
        }
    }

    /// <summary>
    /// Issues the invoices a hook missed (a failure, a charger that never reports the connector free): billed
    /// sessions and paid top-ups of the last <see cref="InvoiceSweeper.LookbackDays"/> days without an invoice.
    /// Older events are invoiced on first download. Runs every 30 minutes.
    /// </summary>
    public sealed class InvoiceSweeper : BackgroundService
    {
        public const int LookbackDays = 7;
        private const int Batch = 200;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<InvoiceSweeper> _logger;

        public InvoiceSweeper(IServiceScopeFactory scopes, ILogger<InvoiceSweeper> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
            do
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    await SweepAsync(scope.ServiceProvider, DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Invoice sweep failed");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        /// <summary>One pass; returns how many documents were issued.</summary>
        public static async Task<int> SweepAsync(IServiceProvider services, DateTime utcNow, CancellationToken ct = default)
        {
            var db = services.GetRequiredService<VoltaXApiDbContext>();
            var invoices = services.GetRequiredService<IInvoiceService>();
            var events = services.GetRequiredService<IBillingEvents>();
            var options = services.GetRequiredService<IOptions<InvoicingOptions>>().Value;
            var since = utcNow.AddDays(-LookbackDays);
            var unsettledBefore = utcNow.AddHours(-Math.Max(1, options.UnsettledSessionDelayHours));

            var sessions = await db.ChargingSessions.AsNoTracking()
                .Where(s => s.EndDate != null && s.EndDate >= since && s.TotalAmount > 0
                    && (s.ChargingSessionStatus == ChargingSessionStatusEnum.Completed || s.EndDate < unsettledBefore)
                    && !db.Invoices.Any(i => i.ChargingSessionID == s.ID && i.SourceReference == "session:" + s.ID))
                .OrderBy(s => s.ID).Select(s => s.ID).Take(Batch).ToListAsync(ct);
            // Idle fees settled after the session invoice was issued.
            var idle = await db.ChargingSessions.AsNoTracking()
                .Where(s => s.EndDate >= since && s.IdleAmount > 0 && s.ChargingSessionStatus == ChargingSessionStatusEnum.Completed
                    && db.Invoices.Any(i => i.ChargingSessionID == s.ID && i.SourceReference == "session:" + s.ID && i.AmountInclVat < s.TotalAmount)
                    && !db.Invoices.Any(i => i.ChargingSessionID == s.ID && i.SourceReference == "session:" + s.ID + ":idle"))
                .OrderBy(s => s.ID).Select(s => s.ID).Take(Batch).ToListAsync(ct);
            var orders = await db.Orders.AsNoTracking()
                .Where(o => (o.Status == RechargeOrderStatus.Completed || o.Status == RechargeOrderStatus.Refunded) && o.Amount > 0
                    && (o.PaidAt ?? o.RechargeDate) >= since
                    && !db.Invoices.Any(i => i.OrderID == o.ID && i.SourceReference == "order:" + o.ID))
                .OrderBy(o => o.ID).Select(o => o.ID).Take(Batch).ToListAsync(ct);

            var issued = 0;
            foreach (var id in sessions.Concat(idle).Distinct())
                if (await invoices.IssueForSessionAsync(id, ct) != null)
                    issued++;
            foreach (var id in orders)
            {
                await events.TopUpCompletedAsync(id, ct);
                issued++;
            }
            return issued;
        }
    }
}
