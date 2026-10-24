using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.Models.Billing;
using VoltaXApi.Services.Payments;

namespace VoltaXApi.Services.Billing
{
    /// <param name="Method">For a top-up: Gateway (default when the provider can refund) or External. Sessions are refunded to the wallet.</param>
    public sealed record RefundRequest(decimal Amount, string Reason, string IdempotencyKey, RefundMethod? Method = null);

    public sealed record RefundDto(int Id, RefundTarget Target, int? OrderId, int? ChargingSessionId, int CardId, decimal Amount, string Reason,
        RefundMethod Method, RefundStatus Status, string? Provider, string? ProviderReference, string? FailureReason, int RequestedByUserId,
        string? RequestedBy, DateTime CreatedAt, DateTime? CompletedAt, int? CreditNoteId, string? CreditNoteNumber);

    /// <summary>What can still be refunded, and how.</summary>
    public sealed record RefundSummary(RefundTarget Target, int Id, decimal Paid, decimal Refunded, decimal Refundable, decimal CardBalance,
        IReadOnlyList<RefundMethod> Methods, RefundMethod? DefaultMethod, string? Provider, string? Note, IReadOnlyList<RefundDto> Refunds);

    /// <summary>A refund that is not allowed; <see cref="Status"/> is the HTTP status to answer.</summary>
    public sealed class RefundRejectedException(int status, string code, string message) : Exception(message)
    {
        public int Status { get; } = status;
        public string Code { get; } = code;
    }

    /// <summary>
    /// Operator refunds. A top-up is refunded to the customer's payment method through the provider (or recorded
    /// as refunded outside the platform) and the amount leaves the wallet; a session is refunded to the wallet.
    /// Each refund is idempotent on its key, never takes more than was paid (checked again on every retry of the
    /// card's optimistic lock, so concurrent refunds cannot both pass), gets a credit note and notifies the customer.
    /// </summary>
    public sealed class RefundService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly IWalletService _wallet;
        private readonly IPaymentGateway _gateway;
        private readonly IInvoiceService _invoices;
        private readonly BillingMailer _mailer;
        private readonly GlobalConfigurations _pricing;
        private readonly ILogger<RefundService> _logger;

        public RefundService(VoltaXApiDbContext db, IWalletService wallet, IPaymentGateway gateway, IInvoiceService invoices, BillingMailer mailer,
            GlobalConfigurations pricing, ILogger<RefundService> logger)
        {
            _db = db;
            _wallet = wallet;
            _gateway = gateway;
            _invoices = invoices;
            _mailer = mailer;
            _pricing = pricing;
            _logger = logger;
        }

        // -------------------------------------------------------------------------------- summaries

        public async Task<RefundSummary?> OrderSummaryAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.Orders.AsNoTracking().Include(o => o.Card).FirstOrDefaultAsync(o => o.ID == orderId, ct);
            if (order == null)
                return null;
            var refunded = await RefundedAsync(r => r.OrderID == orderId, ct);
            var paid = order.Status is RechargeOrderStatus.Completed or RechargeOrderStatus.Refunded ? order.Amount : 0;
            var methods = OrderMethods(order);
            return new RefundSummary(RefundTarget.Order, orderId, paid, refunded, Math.Max(0, paid - refunded), order.Card?.Balance ?? 0,
                methods, methods.FirstOrDefault(), order.Provider, OrderNote(order), await ListAsync(r => r.OrderID == orderId, ct));
        }

        public async Task<RefundSummary?> SessionSummaryAsync(int sessionId, CancellationToken ct = default)
        {
            var session = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters().Include(s => s.Card)
                .FirstOrDefaultAsync(s => s.ID == sessionId && !s.IsDeleted, ct);
            if (session == null)
                return null;
            var paid = Billed(session);
            var refunded = await RefundedAsync(r => r.ChargingSessionID == sessionId, ct);
            return new RefundSummary(RefundTarget.Session, sessionId, paid, refunded, Math.Max(0, paid - refunded), session.Card?.Balance ?? 0,
                paid > 0 ? new[] { RefundMethod.Wallet } : Array.Empty<RefundMethod>(), paid > 0 ? RefundMethod.Wallet : null, null,
                session.EndDate == null ? "The session is still running." : null, await ListAsync(r => r.ChargingSessionID == sessionId, ct));
        }

        public async Task<PagedResult<RefundDto>> SearchAsync(DateTime? from, DateTime? to, RefundStatus? status, int page, int pageSize, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _db.Set<Refund>().AsNoTracking();
            if (from.HasValue) query = query.Where(r => r.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(r => r.CreatedAt < to.Value.Date.AddDays(1));
            if (status.HasValue) query = query.Where(r => r.Status == status);
            var total = await query.CountAsync(ct);
            var items = await Project(query.OrderByDescending(r => r.ID).Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(ct);
            return new PagedResult<RefundDto>(items, page, pageSize, total);
        }

        // -------------------------------------------------------------------------------- refunds

        public async Task<RefundDto> RefundOrderAsync(int orderId, RefundRequest request, int operatorId, CancellationToken ct = default)
        {
            Validate(request);
            if (await ReplayAsync(request, RefundTarget.Order, orderId, ct) is { } replay)
                return replay;
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.ID == orderId, ct)
                ?? throw new RefundRejectedException(404, "not_found", "There is no such top-up.");
            if (order.Status is not (RechargeOrderStatus.Completed or RechargeOrderStatus.Refunded))
                throw new RefundRejectedException(409, "not_paid", "Only a paid top-up can be refunded.");
            var methods = OrderMethods(order);
            var method = request.Method ?? methods.FirstOrDefault();
            if (method == RefundMethod.Gateway && !methods.Contains(RefundMethod.Gateway))
                throw new RefundRejectedException(422, "refund_not_supported", OrderNote(order) ?? "This top-up cannot be refunded through the payment provider.");
            if (method is not (RefundMethod.Gateway or RefundMethod.External))
                throw new RefundRejectedException(400, "invalid_method", "A top-up is refunded to its payment method or outside the platform.");

            // Reserve: the refund and the wallet debit commit together, after checking again what is left.
            var refund = await StageAsync(new Refund
            {
                Target = RefundTarget.Order,
                OrderID = order.ID,
                CardID = order.CardID,
                Amount = request.Amount,
                Reason = request.Reason.Trim(),
                Method = method,
                Status = method == RefundMethod.External ? RefundStatus.Succeeded : RefundStatus.Pending,
                Provider = method == RefundMethod.Gateway ? _gateway.Name : null,
                IdempotencyKey = request.IdempotencyKey,
                RequestedByUserID = operatorId
            }, paid: order.Amount, debitWallet: true, ct);

            if (method == RefundMethod.Gateway && refund.Status == RefundStatus.Pending)
            {
                GatewayRefundResult result;
                try
                {
                    result = await _gateway.RefundAsync(order, refund.Amount, $"refund-{refund.ID}", ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refund {RefundId} of top-up {OrderId} failed at {Provider}", refund.ID, order.ID, _gateway.Name);
                    result = new GatewayRefundResult(GatewayRefundOutcome.Failed, Message: "The payment provider could not be reached.");
                }
                refund.ProviderReference = result.ProviderReference;
                refund.ProviderStatus = result.ProviderStatus;
                if (result.Outcome == GatewayRefundOutcome.Succeeded)
                {
                    refund.Status = RefundStatus.Succeeded;
                    refund.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    // Nothing went back: give the reserved amount back to the wallet.
                    refund.Status = RefundStatus.Failed;
                    refund.CompletedAt = DateTime.UtcNow;
                    refund.FailureReason = Truncate(result.Message ?? result.Outcome.ToString(), 256);
                    await _wallet.SaveWithRetryAsync(async () =>
                    {
                        var card = await _db.Cards.FirstAsync(c => c.ID == refund.CardID, ct);
                        if (!await _db.WalletTransactions.AnyAsync(w => w.Reference == WalletReference(refund) + ":reversal", ct))
                            _wallet.Stage(card, new WalletPosting(refund.Amount, WalletTransactionType.Refund, WalletReference(refund) + ":reversal",
                                $"Refund of top-up {order.ID} failed at the payment provider: amount restored", OrderID: order.ID, CreatedByUserID: operatorId));
                    }, ct);
                }
                await _db.SaveChangesAsync(ct);
            }

            if (refund.Status == RefundStatus.Succeeded)
            {
                if (await RefundedAsync(r => r.OrderID == order.ID, ct) >= order.Amount && order.Status != RechargeOrderStatus.Refunded)
                {
                    order.Status = RechargeOrderStatus.Refunded;
                    await _db.SaveChangesAsync(ct);
                }
                await CompleteAsync(refund, await _invoices.IssueForOrderAsync(order.ID, ct), ct);
            }
            _logger.LogInformation("Refund {RefundId} of top-up {OrderId}: {Amount} by {Method}, {Status}", refund.ID, order.ID, refund.Amount, refund.Method, refund.Status);
            return await GetAsync(refund.ID, ct);
        }

        public async Task<RefundDto> RefundSessionAsync(int sessionId, RefundRequest request, int operatorId, CancellationToken ct = default)
        {
            Validate(request);
            if (await ReplayAsync(request, RefundTarget.Session, sessionId, ct) is { } replay)
                return replay;
            if (request.Method is not (null or RefundMethod.Wallet))
                throw new RefundRejectedException(400, "invalid_method", "A charging session is refunded to the wallet.");
            var session = await _db.ChargingSessions.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.ID == sessionId && !s.IsDeleted, ct)
                ?? throw new RefundRejectedException(404, "not_found", "There is no such charging session.");
            var paid = Billed(session);
            if (session.EndDate == null || paid <= 0)
                throw new RefundRejectedException(409, "not_billed", "Only an ended, billed session can be refunded.");

            var refund = await StageAsync(new Refund
            {
                Target = RefundTarget.Session,
                ChargingSessionID = session.ID,
                CardID = session.CardID,
                UserID = session.UserID,
                Amount = request.Amount,
                Reason = request.Reason.Trim(),
                Method = RefundMethod.Wallet,
                Status = RefundStatus.Succeeded,
                IdempotencyKey = request.IdempotencyKey,
                RequestedByUserID = operatorId
            }, paid, debitWallet: false, ct);

            await CompleteAsync(refund, await _invoices.IssueForSessionAsync(session.ID, ct), ct);
            _logger.LogInformation("Refund {RefundId} of session {SessionId}: {Amount} to the wallet", refund.ID, session.ID, refund.Amount);
            return await GetAsync(refund.ID, ct);
        }

        // -------------------------------------------------------------------------------- internals

        private static void Validate(RefundRequest request)
        {
            if (request.Amount <= 0 || request.Amount != Math.Round(request.Amount, 2))
                throw new RefundRejectedException(400, "invalid_amount", "The amount must be positive, in dirhams and cents.");
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 3 || request.Reason.Length > 256)
                throw new RefundRejectedException(400, "invalid_reason", "Give a reason (3 to 256 characters).");
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length is < 8 or > 64)
                throw new RefundRejectedException(400, "invalid_key", "The idempotency key must have 8 to 64 characters.");
        }

        /// <summary>The refund already made with this key: the same request answers it again; another request is refused.</summary>
        private async Task<RefundDto?> ReplayAsync(RefundRequest request, RefundTarget target, int id, CancellationToken ct)
        {
            var existing = await _db.Set<Refund>().AsNoTracking().FirstOrDefaultAsync(r => r.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing == null)
                return null;
            var same = existing.Target == target && (target == RefundTarget.Order ? existing.OrderID : existing.ChargingSessionID) == id && existing.Amount == request.Amount;
            return same ? await GetAsync(existing.ID, ct)
                : throw new RefundRejectedException(409, "key_reused", "This idempotency key was already used for another refund.");
        }

        /// <summary>
        /// Adds the refund and its wallet movement (debit for a top-up, credit for a session) in one SaveChanges,
        /// checking again on every retry that pending and succeeded refunds stay within what was paid and that the
        /// wallet covers a debit. Concurrent refunds of one card conflict on its balance version and retry.
        /// </summary>
        private async Task<Refund> StageAsync(Refund refund, decimal paid, bool debitWallet, CancellationToken ct)
        {
            refund.CreatedAt = DateTime.UtcNow;
            if (refund.Status == RefundStatus.Succeeded)
                refund.CompletedAt = refund.CreatedAt;
            var reference = WalletReference(refund);
            var (orderId, sessionId) = (refund.OrderID, refund.ChargingSessionID);
            System.Linq.Expressions.Expression<Func<Refund, bool>> sameTarget = orderId != null
                ? r => r.OrderID == orderId
                : r => r.ChargingSessionID == sessionId;
            try
            {
                await _wallet.SaveWithRetryAsync(async () =>
                {
                    if (_db.Entry(refund).State == EntityState.Added)
                        _db.Entry(refund).State = EntityState.Detached;
                    // The card first: the refunds read next are at least as recent as the balance version this save is
                    // conditioned on, so a refund committed in between is either counted here or makes this save retry.
                    var card = await _db.Cards.FirstOrDefaultAsync(c => c.ID == refund.CardID, ct)
                        ?? throw new RefundRejectedException(409, "no_card", "The charging card of this payment no longer exists.");
                    var refunded = await RefundedAsync(sameTarget, ct);
                    if (refund.Amount > paid - refunded)
                        throw new RefundRejectedException(422, "exceeds_paid",
                            $"At most {Math.Max(0, paid - refunded).ToString("0.00", CultureInfo.InvariantCulture)} MAD can still be refunded.");
                    if (debitWallet && card.Balance < refund.Amount)
                        throw new RefundRejectedException(422, "insufficient_balance",
                            $"The wallet holds {card.Balance.ToString("0.00", CultureInfo.InvariantCulture)} MAD: the customer already spent part of this top-up.");
                    refund.UserID ??= card.UserID;
                    _db.Set<Refund>().Add(refund);
                    _wallet.Stage(card, new WalletPosting(debitWallet ? -refund.Amount : refund.Amount, WalletTransactionType.Refund, reference,
                        refund.Target == RefundTarget.Order
                            ? $"Refund of top-up {refund.OrderID} ({(refund.Method == RefundMethod.Gateway ? "to the payment method" : "outside the platform")}): {refund.Reason}"
                            : $"Refund of charging session {refund.ChargingSessionID}: {refund.Reason}",
                        ChargingSessionID: refund.ChargingSessionID, OrderID: refund.OrderID, CreatedByUserID: refund.RequestedByUserID));
                }, ct);
            }
            catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
            {
                // The same key was used concurrently: the first request made the refund.
                foreach (var entry in _db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
                    entry.State = EntityState.Detached;
                var existing = await _db.Set<Refund>().AsNoTracking().FirstOrDefaultAsync(r => r.IdempotencyKey == refund.IdempotencyKey, ct);
                if (existing == null)
                    throw;
                throw new RefundRejectedException(409, "in_progress", "This refund was already requested.");
            }
            return refund;
        }

        /// <summary>Issues the credit note of a succeeded refund, then tells the customer (notification and e-mail).</summary>
        private async Task CompleteAsync(Refund refund, Invoice? invoice, CancellationToken ct)
        {
            Invoice? creditNote = null;
            try
            {
                if (invoice != null)
                {
                    var language = invoice.Language;
                    creditNote = await _invoices.IssueCreditNoteAsync(new CreditNoteRequest($"refund:{refund.ID}", invoice.ID, refund.Amount, refund.Reason,
                        RefundNote(refund, language), refund.ID), ct);
                    refund.CreditNoteID = creditNote.ID;
                    await _db.SaveChangesAsync(ct);
                }
                else
                    _logger.LogWarning("Refund {RefundId} has no invoice to credit", refund.ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The credit note of refund {RefundId} could not be issued", refund.ID);
            }
            await NotifyAsync(refund, creditNote, ct);
        }

        private async Task NotifyAsync(Refund refund, Invoice? creditNote, CancellationToken ct)
        {
            if (refund.UserID == null)
                return;
            try
            {
                var language = creditNote?.Language ?? BillingTexts.French;
                var amount = BillingTexts.Money(refund.Amount, language);
                var number = creditNote?.Number ?? "—";
                var typeId = await _db.NotificationTypes.Where(t => t.Name == "Transaction").Select(t => (int?)t.ID).FirstOrDefaultAsync(ct);
                _db.Notifications.Add(new Notification
                {
                    NotificationTypeID = typeId,
                    ReceiverID = refund.UserID,
                    Url = "/my-dashboard/wallet",
                    Action = "Refund",
                    ActionOn = $"Refund:{refund.ID}",
                    Description = BillingTexts.Format(language, "NotificationRefund", amount, number)
                });
                await _db.SaveChangesAsync(ct);
                if (creditNote != null)
                {
                    var target = refund.Target == RefundTarget.Order ? $"{BillingTexts.Get(language, "Order")} #{refund.OrderID}" : $"{BillingTexts.Get(language, "Session")} #{refund.ChargingSessionID}";
                    await _mailer.SendAsync(creditNote,
                        BillingTexts.Format(language, "MailRefundSubject", amount, number),
                        BillingTexts.Format(language, refund.Method == RefundMethod.Wallet ? "MailRefundWalletBody" : "MailRefundCardBody", amount, target, number), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "The customer of refund {RefundId} could not be notified", refund.ID);
            }
        }

        private List<RefundMethod> OrderMethods(Order order)
        {
            if (order.Status is not (RechargeOrderStatus.Completed or RechargeOrderStatus.Refunded))
                return new List<RefundMethod>();
            var methods = new List<RefundMethod>();
            if (order.Provider != null && order.Provider != "Manual" && string.Equals(order.Provider, _gateway.Name, StringComparison.OrdinalIgnoreCase) && _gateway.SupportsRefunds)
                methods.Add(RefundMethod.Gateway);
            methods.Add(RefundMethod.External);
            return methods;
        }

        private string? OrderNote(Order order) => order.Provider switch
        {
            null or "Manual" => "Manual top-up: refund the customer outside the platform (cash, transfer) and record it here.",
            var p when !string.Equals(p, _gateway.Name, StringComparison.OrdinalIgnoreCase) =>
                $"Paid with {p}, which is no longer the active provider: refund in its back office and record the refund as external.",
            _ when !_gateway.SupportsRefunds =>
                $"{_gateway.Name} cannot refund through the API: refund in its back office and record the refund as external.",
            _ => null
        };

        private decimal Billed(ChargingSession session) =>
            session.EndDate == null ? 0 : session.TotalPriceWithVAT(session.VatRate ?? _pricing.Vat, (int)_pricing.GracePeriod);

        private Task<decimal> RefundedAsync(System.Linq.Expressions.Expression<Func<Refund, bool>> target, CancellationToken ct) =>
            _db.Set<Refund>().AsNoTracking().Where(target).Where(r => r.Status != RefundStatus.Failed).SumAsync(r => r.Amount, ct);

        private Task<List<RefundDto>> ListAsync(System.Linq.Expressions.Expression<Func<Refund, bool>> target, CancellationToken ct) =>
            Project(_db.Set<Refund>().AsNoTracking().Where(target).OrderByDescending(r => r.ID)).ToListAsync(ct);

        private async Task<RefundDto> GetAsync(int id, CancellationToken ct) =>
            await Project(_db.Set<Refund>().AsNoTracking().Where(r => r.ID == id)).FirstAsync(ct);

        private IQueryable<RefundDto> Project(IQueryable<Refund> query) =>
            query.Select(r => new RefundDto(r.ID, r.Target, r.OrderID, r.ChargingSessionID, r.CardID, r.Amount, r.Reason, r.Method, r.Status,
                r.Provider, r.ProviderReference, r.FailureReason, r.RequestedByUserID,
                _db.Users.IgnoreQueryFilters().Where(u => u.ID == r.RequestedByUserID).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                r.CreatedAt, r.CompletedAt, r.CreditNoteID, _db.Invoices.Where(i => i.ID == r.CreditNoteID).Select(i => i.Number).FirstOrDefault()));

        private static string WalletReference(Refund refund) => $"refund:{refund.IdempotencyKey}";

        private static string RefundNote(Refund refund, string language) => refund.Method switch
        {
            RefundMethod.Wallet => BillingTexts.Get(language, "RefundToWallet"),
            RefundMethod.Gateway => BillingTexts.Format(language, "RefundToGateway", refund.Provider ?? ""),
            _ => BillingTexts.Get(language, "RefundExternal")
        };

        private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
    }
}
