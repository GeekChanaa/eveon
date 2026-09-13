using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Models;
using VoltaXApi.Settings;

namespace VoltaXApi.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly VoltaXApiDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IUserClaimsFactory _claimsFactory;
        private readonly AuthTokenSettings _settings;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            VoltaXApiDbContext context,
            IJwtService jwtService,
            IUserClaimsFactory claimsFactory,
            IOptions<AuthTokenSettings> settings,
            ILogger<RefreshTokenService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _context = context;
            _jwtService = jwtService;
            _claimsFactory = claimsFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<(string Token, DateTime ExpiresAt)> Issue(int userID, string? ipAddress, string? userAgent)
        {
            var expiresAt = DateTime.UtcNow.Add(_settings.RefreshTokenLifetime);
            var raw = await Create(userID, expiresAt, ipAddress, userAgent);

            return (raw, expiresAt);
        }

        public async Task<LoginResultDto> Rotate(string rawToken, string? ipAddress, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new UnauthorizedException("A refresh token is required");

            string hash = Hash(rawToken);
            var stored = await _refreshTokenRepository.GetByHash(hash);

            if (stored == null)
                throw new UnauthorizedException("Invalid refresh token");

            // A token presented after it was already spent means the value leaked: the
            // legitimate client and the attacker now both hold one. Cut the whole family.
            if (stored.IsRevoked)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserID} from {IpAddress}; revoking every active session",
                    stored.UserID, ipAddress);

                await RevokeAllForUser(stored.UserID, "reuse");
                throw new UnauthorizedException("This session was closed for security reasons, please sign in again");
            }

            if (stored.IsExpired)
                throw new UnauthorizedException("Your session expired, please sign in again");

            var user = await _context.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.ID == stored.UserID);

            if (user == null || user.IsDeleted)
            {
                await Revoke(rawToken, "user-gone");
                throw new UnauthorizedException("Invalid refresh token");
            }

            if (user.SuspendedAt != null)
            {
                await RevokeAllForUser(user.ID, "suspended");
                throw new UnauthorizedException("This account is suspended");
            }

            // A sliding session restarts the clock; a fixed one keeps the original deadline.
            var successorExpiry = _settings.SlidingExpiration
                ? DateTime.UtcNow.Add(_settings.RefreshTokenLifetime)
                : stored.ExpiresAt;

            string newRaw;
            try
            {
                newRaw = await Create(user.ID, successorExpiry, ipAddress, userAgent, replaces: stored);
            }
            catch (DbUpdateConcurrencyException)
            {
                // SaveChanges rolls back the successor insert as well as the update.
                _context.ChangeTracker.Clear();
                await RevokeAllForUser(user.ID, "reuse");
                throw new UnauthorizedException("This session was already refreshed, please sign in again");
            }

            var accessToken = _jwtService.GenerateAccessToken(_claimsFactory.BuildClaimsFor(user));

            return new LoginResultDto
            {
                Token = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = newRaw,
                RefreshTokenExpiresAt = successorExpiry,
                UserId = user.ID,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}"
            };
        }

        public async Task Revoke(string rawToken, string reason = "logout")
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return;

            var stored = await _refreshTokenRepository.GetByHash(Hash(rawToken));
            if (stored == null || stored.IsRevoked)
                return;

            stored.RevokedAt = DateTime.UtcNow;
            stored.RevokedReason = reason;
            stored.UpdatedAt = DateTime.UtcNow;

            await _refreshTokenRepository.SaveChanges();
        }

        public async Task RevokeAllForUser(int userID, string reason = "revoke-all")
        {
            var active = await _refreshTokenRepository.GetActiveTokensForUser(userID);
            if (active.Count == 0)
                return;

            var now = DateTime.UtcNow;
            foreach (var token in active)
            {
                token.RevokedAt = now;
                token.RevokedReason = reason;
                token.UpdatedAt = now;
            }

            await _refreshTokenRepository.SaveChanges();
        }

        /// <summary>
        /// Stores a new token and, when it succeeds a spent one, closes the predecessor in
        /// the same unit of work so a crash cannot leave two usable tokens behind.
        /// </summary>
        private async Task<string> Create(
            int userID,
            DateTime expiresAt,
            string? ipAddress,
            string? userAgent,
            RefreshToken? replaces = null)
        {
            string raw = GenerateRawToken();
            string hash = Hash(raw);
            var now = DateTime.UtcNow;

            var token = new RefreshToken
            {
                TokenHash = hash,
                UserID = userID,
                ExpiresAt = expiresAt,
                CreatedByIp = Truncate(ipAddress, 64),
                UserAgent = Truncate(userAgent, 256),
                CreatedAt = now,
                UpdatedAt = now
            };

            if (replaces != null)
            {
                replaces.RevokedAt = now;
                replaces.RevokedReason = "rotated";
                replaces.ReplacedByTokenHash = hash;
                replaces.UpdatedAt = now;
            }

            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();

            return raw;
        }

        /// <summary>256 bits of randomness, URL safe so it survives a query string.</summary>
        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        private static string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }

        private static string? Truncate(string? value, int max)
            => value == null || value.Length <= max ? value : value.Substring(0, max);
    }
}
