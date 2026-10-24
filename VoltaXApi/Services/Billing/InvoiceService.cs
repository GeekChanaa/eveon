using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;

namespace VoltaXApi.Services.Billing
{
    /// <summary>Invoicing settings (section "Invoicing").</summary>
    public sealed class InvoicingOptions
    {
        /// <summary>Series of invoices: EV-2026-000123.</summary>
        public string InvoicePrefix { get; set; } = "EV";
        /// <summary>Series of credit notes (avoirs): AV-2026-000001.</summary>
        public string CreditNotePrefix { get; set; } = "AV";
        /// <summary>Language of documents and emails for users without a preference (fr, en, ar).</summary>
        public string DefaultLanguage { get; set; } = BillingTexts.French;
        /// <summary>
        /// A session whose car never left (idle time never settled, charger offline) is invoiced anyway this many
        /// hours after it ended; an idle fee billed later gets its own invoice.
        /// </summary>
        public int UnsettledSessionDelayHours { get; set; } = 24;
    }

    /// <summary>What a credit note credits: amount (VAT included), reason and how the money went back.</summary>
    public sealed record CreditNoteRequest(string SourceReference, int CreditedInvoiceID, decimal Amount, string? Reason, string RefundNote,
        int? RefundID = null);

    /// <summary>
    /// Issues the legal invoices of charging sessions and wallet top-ups and the credit notes of refunds. A
    /// document is issued once per billed event (its source reference is unique), with the next number of its
    /// series for the year, and its PDF is rendered and stored in the same transaction, so a number is never
    /// skipped nor used twice. Downloads return the stored PDF.
    /// </summary>
    public interface IInvoiceService
    {
        /// <summary>
        /// The invoice of an ended, billed session with a positive total, issued on first call. An idle fee billed
        /// after the invoice was issued gets its own invoice (session:{id}:idle). Null when there is nothing to invoice.
        /// </summary>
        Task<Invoice?> IssueForSessionAsync(int sessionId, CancellationToken ct = default);

        /// <summary>The invoice of a paid top-up (Completed or Refunded), issued on first call; null otherwise.</summary>
        Task<Invoice?> IssueForOrderAsync(int orderId, CancellationToken ct = default);

        /// <summary>Issues (once per source reference) a credit note against an issued invoice.</summary>
        Task<Invoice> IssueCreditNoteAsync(CreditNoteRequest request, CancellationToken ct = default);

        Task<InvoiceFile?> GetFileAsync(int invoiceId, CancellationToken ct = default);
    }

    public sealed class InvoiceService : IInvoiceService
    {
        private const int MaxAttempts = 20;
        private readonly IServiceScopeFactory _scopes;
        private readonly VoltaXApiDbContext _db;
        private readonly IBusinessClock _clock;
        private readonly GlobalConfigurations _pricing;
        private readonly InvoicingOptions _options;
        private readonly IOptionsMonitor<CompanyInformations> _company;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(IServiceScopeFactory scopes, VoltaXApiDbContext db, IBusinessClock clock, GlobalConfigurations pricing,
            IOptions<InvoicingOptions> options, IOptionsMonitor<CompanyInformations> company, ILogger<InvoiceService> logger)
        {
            _scopes = scopes;
            _db = db;
            _clock = clock;
            _pricing = pricing;
            _options = options.Value;
            _company = company;
            _logger = logger;
        }

        public static string SessionReference(int sessionId) => $"session:{sessionId}";
        public static string SessionIdleReference(int sessionId) => $"session:{sessionId}:idle";
        public static string OrderReference(int orderId) => $"order:{orderId}";

        public async Task<Invoice?> IssueForSessionAsync(int sessionId, CancellationToken ct = default)
        {
            var row = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters()
                .Where(s => s.ID == sessionId && !s.IsDeleted)
                .Select(s => new
                {
                    Session = s,
                    Station = s.Connector!.ChargePoint!.ChargingStation!.Name,
                    ChargePoint = s.Connector!.ChargePoint!.ChargePointId,
                    s.Connector!.EvseID,
                    Connector = s.Connector!.ConnectorID,
                    CardNumber = s.Card!.CardNumber,
                    CardUserId = s.Card!.UserID
                })
                .FirstOrDefaultAsync(ct);
            if (row?.Session.EndDate == null)
                return null;
            var session = row.Session;
            var vatRate = session.VatRate ?? _pricing.Vat;
            var total = session.TotalPriceWithVAT(vatRate, (int)_pricing.GracePeriod);
            if (total <= 0)
                return null;

            var main = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.SourceReference == SessionReference(sessionId), ct);
            var customer = await CustomerAsync(session.UserID ?? row.CardUserId, ct);
            var references = new List<string>();
            string Ref(string language)
            {
                var start = _clock.ToLocal(session.StartDate);
                var end = _clock.ToLocal(session.EndDate!.Value);
                return $"{BillingTexts.Get(language, "Session")} #{session.ID} · {start:dd/MM/yyyy HH:mm} – {(end.Date == start.Date ? end.ToString("HH:mm", CultureInfo.InvariantCulture) : end.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture))}";
            }
            string Place(string language) => string.Join(" · ", new[]
            {
                row.Station,
                row.ChargePoint == null ? null : $"{BillingTexts.Get(language, "ChargePoint")} {row.ChargePoint}",
                $"{BillingTexts.Get(language, "Connector")} {(row.Connector is null or 1 ? $"EVSE {row.EvseID}" : $"EVSE {row.EvseID} / {row.Connector}")}"
            }.Where(v => !string.IsNullOrWhiteSpace(v)));

            if (main != null)
            {
                // The car left after the invoice was issued: invoice the idle fee on its own.
                var idle = session.IdleAmount ?? 0;
                if (idle > 0 && main.AmountInclVat + idle == total)
                    await IssueAsync(new Draft(InvoiceKind.Invoice, SessionIdleReference(sessionId), customer, CardLast4(row.CardNumber), vatRate, idle,
                        language => new[] { Line(language, BillingTexts.Format(language, "IdleSupplementLine", main.Number), session.IdleMinutes ?? 0, session.PricePerIdleMinute, idle, perMinute: true) },
                        language => new[] { Ref(language), Place(language) },
                        language => BillingTexts.Get(language, "PaidByWallet"))
                    {
                        ChargingSessionID = sessionId,
                        Description = $"Idle fee of session {sessionId}"
                    }, ct);
                return main;
            }

            return await IssueAsync(new Draft(InvoiceKind.Invoice, SessionReference(sessionId), customer, CardLast4(row.CardNumber), vatRate, total,
                language => SessionLines(language, session, session.IdleChargingPriceWithVAT((int)_pricing.GracePeriod)),
                language => new[] { Ref(language), Place(language) },
                language => BillingTexts.Get(language, "PaidByWallet"))
            {
                ChargingSessionID = sessionId,
                Description = $"Charging session {sessionId}"
            }, ct);
        }

        public async Task<Invoice?> IssueForOrderAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.Orders.AsNoTracking().IgnoreQueryFilters()
                .Where(o => o.ID == orderId && !o.IsDeleted)
                .Select(o => new { o.ID, o.Amount, o.Status, o.Provider, o.PaidAt, o.RechargeDate, o.CardID, CardNumber = o.Card!.CardNumber, o.Card.UserID })
                .FirstOrDefaultAsync(ct);
            if (order == null || order.Status is not (RechargeOrderStatus.Completed or RechargeOrderStatus.Refunded) || order.Amount <= 0)
                return null;
            var existing = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.SourceReference == OrderReference(orderId), ct);
            if (existing != null)
                return existing;

            var customer = await CustomerAsync(order.UserID, ct);
            var paidAt = _clock.ToLocal(order.PaidAt ?? order.RechargeDate);
            return await IssueAsync(new Draft(InvoiceKind.Invoice, OrderReference(orderId), customer, CardLast4(order.CardNumber), _pricing.Vat, order.Amount,
                language => new[] { new InvoiceLine(BillingTexts.Get(language, "TopUpLine"), null, "1", BillingTexts.Money(order.Amount, language), order.Amount) },
                language => new[] { $"{BillingTexts.Get(language, "Order")} #{order.ID} · {paidAt:dd/MM/yyyy HH:mm}" },
                language => order.Provider is null or "Manual"
                    ? BillingTexts.Get(language, "PaidManual")
                    : BillingTexts.Format(language, "PaidOnline", order.Provider))
            {
                OrderID = orderId,
                CardID = order.CardID,
                Description = $"Wallet top-up {orderId}"
            }, ct);
        }

        public async Task<Invoice> IssueCreditNoteAsync(CreditNoteRequest request, CancellationToken ct = default)
        {
            var existing = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.SourceReference == request.SourceReference, ct);
            if (existing != null)
                return existing;
            var credited = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.ID == request.CreditedInvoiceID && i.Kind == InvoiceKind.Invoice, ct)
                ?? throw new InvalidOperationException($"Invoice {request.CreditedInvoiceID} does not exist.");
            if (request.Amount <= 0)
                throw new ArgumentException("A credit note needs a positive amount.", nameof(request));

            var customer = credited.UserID.HasValue
                ? await CustomerAsync(credited.UserID, ct)
                : new Customer(null, credited.CustomerName, credited.CustomerEmail, null);
            return await IssueAsync(new Draft(InvoiceKind.CreditNote, request.SourceReference, customer, null, credited.VatRate, request.Amount,
                language => new[] { new InvoiceLine(BillingTexts.Get(language, "RefundLine"), credited.Description, "1", null, request.Amount) },
                _ => Array.Empty<string>(),
                _ => request.RefundNote)
            {
                ChargingSessionID = credited.ChargingSessionID,
                OrderID = credited.OrderID,
                CardID = credited.CardID,
                RefundID = request.RefundID,
                CreditedInvoiceID = credited.ID,
                CreditedInvoiceNumber = credited.Number,
                Reason = request.Reason,
                Description = $"Credit note for {credited.Number}"
            }, ct);
        }

        public Task<InvoiceFile?> GetFileAsync(int invoiceId, CancellationToken ct = default) =>
            _db.InvoiceFiles.AsNoTracking().FirstOrDefaultAsync(f => f.InvoiceID == invoiceId, ct);

        // -------------------------------------------------------------------------------- internals

        private sealed record Customer(int? UserID, string Name, string? Email, string? PreferredLanguage);

        /// <summary>A document to issue; the texts are built per language once the language is known.</summary>
        private sealed record Draft(
            InvoiceKind Kind,
            string SourceReference,
            Customer Customer,
            string? CardLast4,
            decimal VatRate,
            decimal Total,
            Func<string, IReadOnlyList<InvoiceLine>> Lines,
            Func<string, IReadOnlyList<string>> References,
            Func<string, string?> PaymentNote)
        {
            public int? ChargingSessionID { get; init; }
            public int? OrderID { get; init; }
            public int? CardID { get; init; }
            public int? RefundID { get; init; }
            public int? CreditedInvoiceID { get; init; }
            public string? CreditedInvoiceNumber { get; init; }
            public string? Reason { get; init; }
            public string? Description { get; init; }
        }

        private async Task<Customer> CustomerAsync(int? userId, CancellationToken ct)
        {
            if (userId == null)
                return new Customer(null, "", null, null);
            var user = await _db.Users.AsNoTracking().IgnoreQueryFilters().Where(u => u.ID == userId)
                .Select(u => new { u.ID, u.FirstName, u.LastName, u.Email })
                .FirstOrDefaultAsync(ct);
            return user == null
                ? new Customer(null, "", null, null)
                : new Customer(user.ID, $"{user.FirstName} {user.LastName}".Trim(), user.Email, null);
        }

        private string LanguageOf(Customer customer) => BillingTexts.Normalize(customer.PreferredLanguage, BillingTexts.Normalize(_options.DefaultLanguage));

        private async Task<Invoice> IssueAsync(Draft draft, CancellationToken ct)
        {
            var language = LanguageOf(draft.Customer);
            var lines = draft.Lines(language);
            if (lines.Sum(l => l.Amount) != draft.Total)
                throw new InvalidOperationException($"The lines of {draft.SourceReference} do not add up to its total.");
            var (exclVat, vat) = CostCalculator.SplitVat(draft.Total, draft.VatRate);
            var series = draft.Kind == InvoiceKind.CreditNote ? _options.CreditNotePrefix : _options.InvoicePrefix;

            for (var attempt = 1; ; attempt++)
            {
                await using var scope = _scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<VoltaXApiDbContext>();
                var existing = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.SourceReference == draft.SourceReference, ct);
                if (existing != null)
                    return existing;

                var issuedAt = _clock.UtcNow;
                var local = _clock.ToLocal(issuedAt);
                var sequence = await db.InvoiceSequences.FirstOrDefaultAsync(s => s.Series == series && s.Year == local.Year, ct);
                if (sequence == null)
                    db.InvoiceSequences.Add(sequence = new InvoiceSequence { Series = series, Year = local.Year, LastNumber = 1 });
                else
                    sequence.LastNumber++;
                var number = $"{series}-{local.Year}-{sequence.LastNumber.ToString("000000", CultureInfo.InvariantCulture)}";

                var pdf = InvoicePdf.Render(new InvoicePdfModel(draft.Kind, number, local, language, PartnerPortal.PartnerPortalService.Currency,
                    draft.Customer.Name, draft.Customer.Email, draft.CardLast4, draft.References(language), lines, draft.VatRate, exclVat, vat,
                    draft.Total, draft.PaymentNote(language), draft.CreditedInvoiceNumber, draft.Reason), _company.CurrentValue);
                var invoice = new Invoice
                {
                    Kind = draft.Kind,
                    Number = number,
                    Series = series,
                    Year = local.Year,
                    Sequence = sequence.LastNumber,
                    SourceReference = draft.SourceReference,
                    IssuedAt = issuedAt,
                    ChargingSessionID = draft.ChargingSessionID,
                    OrderID = draft.OrderID,
                    RefundID = draft.RefundID,
                    CreditedInvoiceID = draft.CreditedInvoiceID,
                    UserID = draft.Customer.UserID,
                    CardID = draft.CardID,
                    CustomerName = Truncate(draft.Customer.Name, 200),
                    CustomerEmail = draft.Customer.Email,
                    Language = language,
                    Currency = PartnerPortal.PartnerPortalService.Currency,
                    VatRate = draft.VatRate,
                    AmountExclVat = exclVat,
                    VatAmount = vat,
                    AmountInclVat = draft.Total,
                    Description = draft.Description == null ? null : Truncate(draft.Description, 256),
                    File = new InvoiceFile { Content = pdf, FileName = $"{number}.pdf" }
                };
                db.Invoices.Add(invoice);
                try
                {
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Issued {Kind} {Number} for {Source}: {Amount} MAD", draft.Kind, number, draft.SourceReference, draft.Total);
                    invoice.File = null;
                    return invoice;
                }
                catch (DbUpdateException ex) when (attempt < MaxAttempts)
                {
                    // Another issuer took the number (or issued this very document): start again with fresh data.
                    _logger.LogInformation(ex, "Issuing {Source} collided with a concurrent issue (attempt {Attempt}), retrying", draft.SourceReference, attempt);
                    await Task.Delay(Random.Shared.Next(5, 20 * attempt), ct);
                }
            }
        }

        private static IReadOnlyList<InvoiceLine> SessionLines(string language, ChargingSession s, decimal idle)
        {
            var lines = new List<InvoiceLine>();
            if (s.IsBilled)
            {
                if ((s.EnergyAmount ?? 0) > 0 || (s.PricePerKWh ?? 0) > 0)
                    lines.Add(new InvoiceLine(BillingTexts.Get(language, "EnergyLine"), null,
                        $"{(s.ChargedKwhs ?? 0).ToString("N3", BillingTexts.NumberCulture(language))} kWh",
                        $"{BillingTexts.Money(s.PricePerKWh ?? 0, language)} {BillingTexts.Get(language, "PerKwh")}", s.EnergyAmount ?? 0));
                if ((s.TimeAmount ?? 0) > 0)
                    lines.Add(Line(language, BillingTexts.Get(language, "TimeLine"), s.ChargedMinutes ?? 0, s.PricePerMinute, s.TimeAmount ?? 0, perMinute: true));
                if ((s.FlatFeeAmount ?? 0) > 0)
                    lines.Add(new InvoiceLine(BillingTexts.Get(language, "SessionFeeLine"), null, "1", BillingTexts.Money(s.FlatFee ?? 0, language), s.FlatFeeAmount ?? 0));
            }
            else
                lines.Add(Line(language, BillingTexts.Get(language, "LegacyTimeLine"), s.ChargedMinutes ?? 0, s.PricePerMinute, s.ChargingPriceWithVAT, perMinute: true));
            if (idle > 0)
                lines.Add(Line(language, BillingTexts.Get(language, "IdleLine"), s.IdleMinutes ?? 0, s.PricePerIdleMinute, idle, perMinute: true));
            return lines;
        }

        private static InvoiceLine Line(string language, string description, double minutes, decimal? pricePerMinute, decimal amount, bool perMinute) =>
            new(description, null, $"{minutes.ToString("N1", BillingTexts.NumberCulture(language))} {BillingTexts.Get(language, "Minutes")}",
                pricePerMinute == null ? null : $"{BillingTexts.Money(pricePerMinute.Value, language)} {BillingTexts.Get(language, "PerMinute")}", amount);

        private static string? CardLast4(string? number) => string.IsNullOrEmpty(number) ? null : number.Length >= 4 ? number[^4..] : number;

        private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
    }
}
