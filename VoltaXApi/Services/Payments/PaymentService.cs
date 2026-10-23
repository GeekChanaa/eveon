using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;

namespace VoltaXApi.Services.Payments
{
    public sealed record TopUpStarted(int OrderId, CheckoutInstruction Checkout);

    public sealed record TopUpStatus(int OrderId, int CardId, decimal Amount, RechargeOrderStatus Status, string? Provider,
        DateTime CreatedAt, DateTime? PaidAt, string? FailureReason);

    public sealed record PaymentMethodDto(int Id, DebitCardTypeEnum Brand, string Last4, int ExpiryMonth, int ExpiryYear, bool IsDefault, string? Provider);

    /// <summary>
    /// Online wallet top-ups and saved payment cards. A top-up is created Pending; only a verified provider
    /// notification completes it, and the card is credited through the wallet ledger (reference
    /// order:{id}:topup), so a notification delivered twice credits once.
    /// </summary>
    public sealed class PaymentService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly IPaymentGateway _gateway;
        private readonly IWalletService _wallet;
        private readonly PaymentOptions _options;
        private readonly ILogger<PaymentService> _logger;
        private readonly VoltaXApi.Services.Billing.IBillingEvents _billing;

        public PaymentService(VoltaXApiDbContext db, IPaymentGateway gateway, IWalletService wallet, IOptions<PaymentOptions> options,
            ILogger<PaymentService> logger, VoltaXApi.Services.Billing.IBillingEvents billing)
        {
            _billing = billing;
            _db = db;
            _gateway = gateway;
            _wallet = wallet;
            _options = options.Value;
            _logger = logger;
        }

        public IPaymentGateway Gateway => _gateway;
        public PaymentOptions Options => _options;

        public async Task<TopUpStarted> StartTopUpAsync(User user, int? cardId, decimal amount, CancellationToken cancellationToken = default)
        {
            if (_gateway is DisabledPaymentGateway)
                throw new PaymentUnavailableException();
            if (amount != Math.Round(amount, 2) || amount < _options.MinTopUp || amount > _options.MaxTopUp)
                throw new ValidationException($"The amount must be between {_options.MinTopUp:0.00} and {_options.MaxTopUp:0.00} {_options.Currency}.");

            var cards = _db.Cards.Where(c => c.UserID == user.ID && c.Blocked != true && c.ExpirationDate > DateTime.UtcNow);
            var card = cardId.HasValue
                ? await cards.FirstOrDefaultAsync(c => c.ID == cardId.Value, cancellationToken)
                : await cards.OrderBy(c => c.ID).FirstOrDefaultAsync(cancellationToken);
            if (card == null)
                throw new NotFoundException("No active charging card of yours was found to top up.");

            var order = new Order
            {
                CardID = card.ID,
                Amount = amount,
                Status = RechargeOrderStatus.Pending,
                RechargeDate = DateTime.UtcNow,
                Provider = _gateway.Name
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync(cancellationToken);

            var checkout = await _gateway.CreateCheckoutAsync(order, user, cancellationToken);
            if (checkout.ProviderReference != null)
            {
                order.ProviderReference = checkout.ProviderReference;
                await _db.SaveChangesAsync(cancellationToken);
            }
            _logger.LogInformation("Top-up {OrderId} of {Amount} started for user {UserId} with {Provider}", order.ID, amount, user.ID, _gateway.Name);
            return new TopUpStarted(order.ID, checkout);
        }

        public Task<TopUpStatus?> GetTopUpAsync(int userId, int orderId, CancellationToken cancellationToken = default) =>
            _db.Orders.AsNoTracking()
                .Where(o => o.ID == orderId && o.Card!.UserID == userId)
                .Select(o => new TopUpStatus(o.ID, o.CardID, o.Amount, o.Status, o.Provider, o.CreatedAt, o.PaidAt, o.FailureReason))
                .FirstOrDefaultAsync(cancellationToken);

        /// <summary>
        /// Applies a verified notification once. Returns false when it is refused (unknown order, other provider,
        /// amount mismatch): the provider is then told the notification was not accepted.
        /// </summary>
        public async Task<bool> HandleNotificationAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
        {
            if (await _db.PaymentEvents.AnyAsync(e => e.Provider == _gateway.Name && e.EventId == notification.EventId, cancellationToken))
                return true;

            var result = notification.Outcome switch
            {
                PaymentOutcome.Paid => await CompleteAsync(notification, cancellationToken),
                PaymentOutcome.Failed or PaymentOutcome.Canceled => await FailAsync(notification, cancellationToken),
                PaymentOutcome.CardSaved => await SaveCardAsync(notification, cancellationToken),
                _ => "ignored"
            };

            _db.PaymentEvents.Add(new PaymentEvent
            {
                Provider = _gateway.Name,
                EventId = notification.EventId,
                Type = notification.Type,
                OrderID = notification.OrderId,
                Result = result,
                ReceivedAt = DateTime.UtcNow
            });
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The same notification processed concurrently: the ledger reference kept the credit single.
                if (!await _db.PaymentEvents.AsNoTracking().AnyAsync(e => e.Provider == _gateway.Name && e.EventId == notification.EventId, cancellationToken))
                    throw;
            }
            _logger.LogInformation("Payment notification {EventId} ({Type}) from {Provider}: {Result}", notification.EventId, notification.Type, _gateway.Name, result);
            if (result == "credited" && notification.OrderId.HasValue)
                await _billing.TopUpCompletedAsync(notification.OrderId.Value, cancellationToken);
            return !result.StartsWith("refused", StringComparison.Ordinal);
        }

        private async Task<string> CompleteAsync(PaymentNotification notification, CancellationToken cancellationToken)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.ID == notification.OrderId, cancellationToken);
            if (order == null || order.Provider != _gateway.Name)
                return "refused: unknown order";
            if (notification.Amount.HasValue && notification.Amount.Value != order.Amount)
            {
                _logger.LogWarning("Top-up {OrderId}: paid amount {Paid} differs from the order amount {Amount}; not credited", order.ID, notification.Amount, order.Amount);
                return "refused: amount mismatch";
            }
            if (order.Status == RechargeOrderStatus.Completed)
                return "already completed";

            order.Status = RechargeOrderStatus.Completed;
            order.PaidAt = DateTime.UtcNow;
            order.FailureReason = null;
            if (!string.IsNullOrEmpty(notification.ProviderReference))
                order.ProviderReference = notification.ProviderReference;
            await _wallet.SaveWithRetryAsync(async () =>
            {
                var card = await _db.Cards.FirstAsync(c => c.ID == order.CardID, cancellationToken);
                if (!await _db.WalletTransactions.AnyAsync(w => w.Reference == $"order:{order.ID}:topup", cancellationToken))
                    _wallet.Stage(card, new WalletPosting(order.Amount, WalletTransactionType.TopUp, $"order:{order.ID}:topup",
                        $"Online top-up ({_gateway.Name})", OrderID: order.ID));
            }, cancellationToken);
            return "credited";
        }

        private async Task<string> FailAsync(PaymentNotification notification, CancellationToken cancellationToken)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.ID == notification.OrderId, cancellationToken);
            if (order == null || order.Provider != _gateway.Name)
                return "refused: unknown order";
            if (order.Status is RechargeOrderStatus.Completed or RechargeOrderStatus.Refunded)
                return "ignored: already completed";
            order.Status = notification.Outcome == PaymentOutcome.Canceled ? RechargeOrderStatus.Canceled : RechargeOrderStatus.Failed;
            order.FailureReason = notification.FailureReason?[..Math.Min(notification.FailureReason.Length, 256)];
            return order.Status.ToString().ToLowerInvariant();
        }

        private async Task<string> SaveCardAsync(PaymentNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Card == null || notification.UserId == null ||
                !await _db.Users.AnyAsync(u => u.ID == notification.UserId, cancellationToken))
                return "refused: unknown user";
            if (await _db.DebitCards.AnyAsync(c => c.UserID == notification.UserId && c.ProviderToken == notification.Card.Token, cancellationToken))
                return "card already saved";
            var first = !await _db.DebitCards.AnyAsync(c => c.UserID == notification.UserId, cancellationToken);
            _db.DebitCards.Add(new DebitCard
            {
                UserID = notification.UserId,
                Brand = notification.Card.Brand,
                Last4 = notification.Card.Last4,
                ExpiryMonth = notification.Card.ExpiryMonth,
                ExpiryYear = notification.Card.ExpiryYear,
                Provider = _gateway.Name,
                ProviderToken = notification.Card.Token,
                IsDefault = first
            });
            return "card saved";
        }

        public Task<List<PaymentMethodDto>> ListMethodsAsync(int userId, CancellationToken cancellationToken = default) =>
            _db.DebitCards.AsNoTracking()
                .Where(c => c.UserID == userId)
                .OrderByDescending(c => c.IsDefault).ThenByDescending(c => c.ID)
                .Select(c => new PaymentMethodDto(c.ID, c.Brand, c.Last4, c.ExpiryMonth, c.ExpiryYear, c.IsDefault, c.Provider))
                .ToListAsync(cancellationToken);

        public async Task<CheckoutInstruction> StartCardSetupAsync(User user, CancellationToken cancellationToken = default)
        {
            if (!_gateway.SupportsSavedCards)
                throw new ValidationException("Saving a card is not available with the current payment provider.");
            return await _gateway.CreateCardSetupAsync(user, cancellationToken);
        }

        public async Task<bool> RemoveMethodAsync(int userId, int cardId, CancellationToken cancellationToken = default)
        {
            var card = await _db.DebitCards.FirstOrDefaultAsync(c => c.ID == cardId && c.UserID == userId, cancellationToken);
            if (card == null)
                return false;
            _db.DebitCards.Remove(card);
            if (card.IsDefault)
            {
                var next = await _db.DebitCards.Where(c => c.UserID == userId && c.ID != cardId).OrderByDescending(c => c.ID).FirstOrDefaultAsync(cancellationToken);
                if (next != null) next.IsDefault = true;
                card.IsDefault = false;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SetDefaultMethodAsync(int userId, int cardId, CancellationToken cancellationToken = default)
        {
            var cards = await _db.DebitCards.Where(c => c.UserID == userId).ToListAsync(cancellationToken);
            if (cards.All(c => c.ID != cardId))
                return false;
            foreach (var card in cards)
                card.IsDefault = card.ID == cardId;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
