using VoltaXApi.ScaleOut;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Authorization;
using VoltaXApi.Data;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Services.Gdpr
{
    public class AccountDeletionException : Exception
    {
        public AccountDeletionException(string message, bool invalidCredentials = false) : base(message)
            => InvalidCredentials = invalidCredentials;
        public bool InvalidCredentials { get; }
    }

    public record PendingAccountDeletionDto(int ID, int UserID, string FullName, string Email, DateTime RequestedAt,
        DateTime ScheduledFor, AccountDeletionStatusEnum Status, double WalletBalance, bool RefundRequired, DateTime? CompletedAt);

    public interface IAccountDeletionService
    {
        Task<AccountDeletionRequest> RequestAsync(int userId, string? password, string? ipAddress);
        Task CancelAsync(int userId, int? cancelledByUserId);
        Task<List<PendingAccountDeletionDto>> ListAsync(AccountDeletionStatusEnum status);
        Task<int> AnonymizeDueAsync(CancellationToken cancellationToken);
    }

    public class AccountDeletionService : IAccountDeletionService
    {
        public const string SuspensionReason = "Account deletion requested";
        // Login and every API call check SuspendedAt; a far-future value blocks both immediately.
        private static readonly DateTime LoginDisabledUntil = new(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        private readonly VoltaXApiDbContext _db;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly HubConnections _hubConnections;
        private readonly IAuditLogger _audit;
        private readonly IUserDataExportService _exports;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountDeletionService> _logger;

        public AccountDeletionService(VoltaXApiDbContext db, IRefreshTokenService refreshTokens, HubConnections hubConnections,
            IAuditLogger audit, IUserDataExportService exports, IConfiguration configuration, ILogger<AccountDeletionService> logger)
        {
            _db = db;
            _refreshTokens = refreshTokens;
            _hubConnections = hubConnections;
            _audit = audit;
            _exports = exports;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AccountDeletionRequest> RequestAsync(int userId, string? password, string? ipAddress)
        {
            var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.ID == userId)
                ?? throw new AccountDeletionException("Account not found.");
            if (user.DeletionRequestedAt != null)
                throw new AccountDeletionException("Account deletion is already scheduled.");
            if (user.Role?.Name == "Admin")
                throw new AccountDeletionException("Administrator accounts must be demoted before they can be deleted.");
            // Google-only accounts have no password; their recent sign-in is the proof of ownership.
            if (user.HasPassword && (string.IsNullOrEmpty(password) || !AuthHelper.VerifyPasswordHash(password, user.PasswordHash!, user.PasswordSalt!)))
                throw new AccountDeletionException("The password is incorrect.", invalidCredentials: true);

            var now = DateTime.UtcNow;
            var graceDays = Math.Max(0, _configuration.GetValue("Gdpr:DeletionGraceDays", 14));

            var cards = await _db.Cards.Where(c => c.UserID == userId).ToListAsync();
            var blocked = new List<int>();
            foreach (var card in cards.Where(c => c.Status != CardStatusEnum.Blocked))
            {
                card.Status = CardStatusEnum.Blocked;
                card.Blocked = true;
                blocked.Add(card.ID);
            }
            var balance = Math.Round(cards.Sum(c => c.Balance), 2);

            user.DeletionRequestedAt = now;
            if (user.SuspendedAt == null)
            {
                user.SuspendedAt = LoginDisabledUntil;
                user.SuspensionReason = SuspensionReason;
            }

            var request = new AccountDeletionRequest
            {
                UserID = userId,
                RequestedAt = now,
                ScheduledFor = now.AddDays(graceDays),
                WalletBalance = balance,
                RefundRequired = balance > 0,
                BlockedCardIDs = blocked.Count == 0 ? null : string.Join(",", blocked),
                RequestedFromIp = ipAddress?.Length > 64 ? ipAddress[..64] : ipAddress
            };
            _db.AccountDeletionRequests.Add(request);
            await _db.SaveChangesAsync();

            await _refreshTokens.RevokeAllForUser(userId, "account-deletion");
            _hubConnections.Revoke(new[] { userId });
            await _audit.LogForUserAsync("AccountDeletionRequested", userId, user.Email, "User", userId.ToString(),
                new { request.ScheduledFor, request.WalletBalance });
            return request;
        }

        public async Task CancelAsync(int userId, int? cancelledByUserId)
        {
            var request = await _db.AccountDeletionRequests
                .Where(r => r.UserID == userId && r.Status == AccountDeletionStatusEnum.Pending)
                .OrderByDescending(r => r.RequestedAt).FirstOrDefaultAsync()
                ?? throw new AccountDeletionException("No pending deletion for this account.");
            var user = await _db.Users.SingleAsync(u => u.ID == userId);

            request.Status = AccountDeletionStatusEnum.Cancelled;
            request.CancelledAt = DateTime.UtcNow;
            request.CancelledByUserID = cancelledByUserId;
            user.DeletionRequestedAt = null;
            if (user.SuspensionReason == SuspensionReason)
            {
                user.SuspendedAt = null;
                user.SuspensionReason = null;
            }

            var cardIds = (request.BlockedCardIDs ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var v) ? v : 0).Where(v => v > 0).ToList();
            if (cardIds.Count > 0)
            {
                var cards = await _db.Cards.Where(c => cardIds.Contains(c.ID) && c.UserID == userId && c.Status == CardStatusEnum.Blocked).ToListAsync();
                foreach (var card in cards)
                {
                    card.Status = CardStatusEnum.Active;
                    card.Blocked = false;
                }
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync("AccountDeletionCancelled", "User", userId.ToString());
        }

        public Task<List<PendingAccountDeletionDto>> ListAsync(AccountDeletionStatusEnum status) =>
            _db.AccountDeletionRequests.AsNoTracking()
                .Where(r => r.Status == status)
                .OrderBy(r => r.ScheduledFor)
                .Select(r => new PendingAccountDeletionDto(r.ID, r.UserID, r.User!.FirstName + " " + r.User.LastName, r.User.Email,
                    r.RequestedAt, r.ScheduledFor, r.Status, r.WalletBalance, r.RefundRequired, r.CompletedAt))
                .ToListAsync();

        public async Task<int> AnonymizeDueAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var due = await _db.AccountDeletionRequests
                .Where(r => r.Status == AccountDeletionStatusEnum.Pending && r.ScheduledFor <= now)
                .OrderBy(r => r.ScheduledFor).Select(r => r.ID).Take(50).ToListAsync(cancellationToken);

            var done = 0;
            foreach (var id in due)
            {
                try
                {
                    await AnonymizeAsync(id, cancellationToken);
                    done++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Anonymization of deletion request {RequestID} failed", id);
                    _db.ChangeTracker.Clear();
                }
            }
            return done;
        }

        // Sessions, orders, transactions and cards stay for accounting; the user row they point
        // to no longer carries anything that identifies a person.
        private async Task AnonymizeAsync(int requestId, CancellationToken ct)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            var request = await _db.AccountDeletionRequests.SingleAsync(r => r.ID == requestId, ct);
            var userId = request.UserID;
            var user = await _db.Users.IgnoreQueryFilters().SingleAsync(u => u.ID == userId, ct);
            var phone = user.Phone;
            var debitCardIds = await _db.DebitCards.IgnoreQueryFilters().Where(d => d.UserID == userId).Select(d => d.ID).ToListAsync(ct);
            var exportFiles = await _db.UserInfoDownloadRequests.IgnoreQueryFilters()
                .Where(r => r.UserID == userId && r.ExportFileName != null).Select(r => r.ExportFileName).ToListAsync(ct);

            user.FirstName = "Deleted";
            user.LastName = "User";
            user.Email = $"deleted-{Guid.NewGuid():N}@invalid";
            user.Phone = null;
            user.Gender = null;
            user.City = null;
            user.Birthday = null;
            user.ElectricVehicleModelID = null;
            user.ImageID = null;
            user.GoogleId = null;
            user.ExternalPictureUrl = null;
            user.AuthProvider = AuthProviderEnum.Local;
            user.PasswordHash = null;
            user.PasswordSalt = null;
            user.EmailVerificationToken = null;
            user.PhoneVerificationToken = null;
            user.ResetPasswordCode = null;
            user.ResetPasswordCodeExpiresAt = null;
            user.ResetPasswordToken = null;
            user.IsEmailVerified = false;
            user.IsPhoneNumberVerified = false;
            user.DeletedAt = DateTime.UtcNow;
            user.SuspendedAt = LoginDisabledUntil;
            user.SuspensionReason = "Account deleted";

            request.Status = AccountDeletionStatusEnum.Completed;
            request.CompletedAt = DateTime.UtcNow;
            request.RequestedFromIp = null;
            await _db.SaveChangesAsync(ct);

            // Hard deletes: soft-deleted rows would keep the PAN / CVV in the database.
            await _db.DebitCards.IgnoreQueryFilters().Where(d => d.UserID == userId).ExecuteDeleteAsync(ct);
            await _db.RefreshTokens.IgnoreQueryFilters().Where(t => t.UserID == userId).ExecuteDeleteAsync(ct);
            await _db.Notifications.IgnoreQueryFilters().Where(n => n.ReceiverID == userId).ExecuteDeleteAsync(ct);
            await _db.NotificationSettings.IgnoreQueryFilters().Where(n => n.UserID == userId).ExecuteDeleteAsync(ct);
            if (!string.IsNullOrEmpty(phone))
                await _db.PhoneLoginChallenges.Where(c => c.Phone == phone).ExecuteDeleteAsync(ct);
            await _db.UserInfoDownloadRequests.IgnoreQueryFilters().Where(r => r.UserID == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ExportFileName, (string?)null).SetProperty(r => r.DownloadTokenHash, (string?)null), ct);

            // The audit trail keeps what happened, not who it was.
            var userKey = userId.ToString();
            var debitKeys = debitCardIds.Select(id => id.ToString()).ToList();
            await _db.AuditLogs.Where(a => a.UserID == userId).ExecuteUpdateAsync(s => s.SetProperty(a => a.UserEmail, (string?)null), ct);
            await _db.AuditLogs.Where(a => a.EntityType == nameof(User) && a.EntityID == userKey ||
                                            a.EntityType == nameof(DebitCard) && debitKeys.Contains(a.EntityID!))
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ChangesJson, (string?)null), ct);

            await transaction.CommitAsync(ct);

            foreach (var file in exportFiles)
                _exports.DeleteArchive(file);
            await _audit.LogForUserAsync("AccountAnonymized", userId, null, nameof(User), userId.ToString(),
                new { requestId, request.WalletBalance, request.RefundRequired });

            _logger.LogInformation("Account {UserID} anonymized (deletion request {RequestID}, wallet balance {Balance})",
                userId, requestId, request.WalletBalance);
        }
    }

    /// <summary>Runs the anonymization of accounts whose grace period is over, once a day.</summary>
    public class AccountDeletionWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AccountDeletionWorker> _logger;
        private readonly IClusterJobLease _lease;

        public AccountDeletionWorker(IServiceScopeFactory scopeFactory, ILogger<AccountDeletionWorker> logger, IClusterJobLease lease)
        {
            _lease = lease;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(StartupDelay, stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only one replica runs this job (see ScaleOut/ClusterJobLease.cs).
                    if (await _lease.TryAcquireAsync("account-anonymization", Interval + TimeSpan.FromHours(1), stoppingToken))
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<IAccountDeletionService>();
                        var count = await service.AnonymizeDueAsync(stoppingToken);
                        if (count > 0)
                            _logger.LogInformation("Anonymized {Count} deleted accounts", count);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Account anonymization sweep failed");
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }
    }
}
