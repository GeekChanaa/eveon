using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public sealed record WalletPosting(decimal Amount, WalletTransactionType Type, string Reference, string? Description = null,
        int? ChargingSessionID = null, int? OrderID = null, int? CreatedByUserID = null);

    /// <summary>
    /// The only way a card balance changes. Each change is a <see cref="WalletTransaction"/> saved in the same
    /// SaveChanges as the new balance; <see cref="Card.BalanceVersion"/> makes concurrent changes of one card
    /// (a top-up while a session is billed) retry instead of overwriting each other.
    /// </summary>
    public interface IWalletService
    {
        /// <summary>Posts one movement and saves. Idempotent on <see cref="WalletPosting.Reference"/>.</summary>
        Task<WalletTransaction> PostAsync(int cardId, WalletPosting posting, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the movement and the new balance to the tracked <paramref name="card"/> without saving, so a caller
        /// can commit it together with its own changes (see <see cref="SaveWithRetryAsync"/>).
        /// </summary>
        WalletTransaction Stage(Card card, WalletPosting posting);

        /// <summary>
        /// Runs <paramref name="stage"/> then SaveChanges; on a concurrency conflict the tracked state is reloaded
        /// and <paramref name="stage"/> runs again (it must be re-entrant: read balances from the reloaded entities).
        /// </summary>
        Task SaveWithRetryAsync(Func<Task> stage, CancellationToken cancellationToken = default);

        Task<WalletTransaction?> FindAsync(string reference, CancellationToken cancellationToken = default);
    }

    public sealed class WalletService : IWalletService
    {
        private const int MaxAttempts = 5;
        private readonly VoltaXApiDbContext _db;
        private readonly ILogger<WalletService> _logger;

        public WalletService(VoltaXApiDbContext db, ILogger<WalletService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public Task<WalletTransaction?> FindAsync(string reference, CancellationToken cancellationToken = default) =>
            _db.WalletTransactions.AsNoTracking().FirstOrDefaultAsync(w => w.Reference == reference, cancellationToken);

        public async Task<WalletTransaction> PostAsync(int cardId, WalletPosting posting, CancellationToken cancellationToken = default)
        {
            WalletTransaction? movement = null;
            try
            {
                await SaveWithRetryAsync(async () =>
                {
                    movement = null;
                    if (await _db.WalletTransactions.AnyAsync(w => w.Reference == posting.Reference, cancellationToken))
                        return;
                    var card = await _db.Cards.FirstOrDefaultAsync(c => c.ID == cardId, cancellationToken)
                        ?? throw new CardNotFoundException($"There is no card with the id {cardId}");
                    movement = Stage(card, posting);
                }, cancellationToken);
            }
            catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
            {
                // Lost the race on the unique Reference: the other request posted it.
                DetachPending();
                var existing = await FindAsync(posting.Reference, cancellationToken);
                if (existing == null) throw;
                _logger.LogInformation("Wallet movement {Reference} was posted concurrently; the first one is kept", posting.Reference);
                return existing;
            }

            return movement ?? (await FindAsync(posting.Reference, cancellationToken))!;
        }

        public WalletTransaction Stage(Card card, WalletPosting posting)
        {
            if (posting.Amount == 0)
                throw new ArgumentException("A wallet movement needs a non-zero amount", nameof(posting));
            var amount = Math.Round(posting.Amount, 2, MidpointRounding.AwayFromZero);
            card.Balance += amount;
            card.BalanceVersion++;
            var movement = new WalletTransaction
            {
                CardID = card.ID,
                Type = posting.Type,
                Amount = amount,
                BalanceAfter = card.Balance,
                ChargingSessionID = posting.ChargingSessionID,
                OrderID = posting.OrderID,
                Reference = posting.Reference,
                Description = posting.Description,
                CreatedByUserID = posting.CreatedByUserID,
                CreatedAt = DateTime.UtcNow
            };
            _db.WalletTransactions.Add(movement);
            return movement;
        }

        public async Task SaveWithRetryAsync(Func<Task> stage, CancellationToken cancellationToken = default)
        {
            for (var attempt = 1; ; attempt++)
            {
                await stage();
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    return;
                }
                catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
                {
                    _logger.LogInformation("Card balance changed concurrently, retrying (attempt {Attempt})", attempt);
                    foreach (var entry in ex.Entries)
                        await entry.ReloadAsync(cancellationToken);
                    DetachPending();
                }
            }
        }

        /// <summary>
        /// Drops the rows a failed SaveChanges left behind (staged movements, card history added by the context),
        /// so a retry does not insert them twice.
        /// </summary>
        private void DetachPending()
        {
            foreach (var entry in _db.ChangeTracker.Entries()
                         .Where(e => e.State == EntityState.Added && e.Entity is WalletTransaction or CardChangeHistory).ToList())
                entry.State = EntityState.Detached;
        }
    }
}
