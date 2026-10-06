using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Helpers;
using System.Security.Claims;

namespace VoltaXApi.Services
{
    public class PartnerAuthService : IPartnerAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMailService _mailService;
        private readonly VoltaXApiDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILoginAttemptRepository _loginAttemptRepository;
        private readonly IMailRequestFactory _mailRequestFactory;
        private readonly ICardRepository _cardRepository;
        private readonly ISnsService _snsService;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IUserClaimsFactory _claimsFactory;
        private readonly IPasswordResetService _passwordReset;
        private readonly ITwoFactorService _twoFactor;

        public PartnerAuthService(
          IUserRepository userRepository,
          IMailService mailService,
          IConfiguration config,
          VoltaXApiDbContext context,
          ILoginAttemptRepository loginAttemptRepository,
          IMailRequestFactory mailRequestFactory,
          ICardRepository cardRepository,
          ISnsService snsService,
          IJwtService jwtService,
          IRefreshTokenService refreshTokenService,
          IUserClaimsFactory claimsFactory,
          IPasswordResetService passwordReset,
          ITwoFactorService twoFactor
        )
        {
            _passwordReset = passwordReset;
            _twoFactor = twoFactor;
            _userRepository = userRepository;
            _mailService = mailService;
            _context = context;
            _config = config;
            _loginAttemptRepository = loginAttemptRepository;
            _mailRequestFactory = mailRequestFactory;
            _cardRepository = cardRepository;
            _snsService = snsService;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _claimsFactory = claimsFactory;
        }

        // Emails a single use, 30 minute reset link. Never reveals whether the address exists.
        public Task RequestPasswordReset(string email) => _passwordReset.RequestLinkReset(email, partnerPortal: true);

        public Task ResetPassword(string email, string token, string newPassword) => _passwordReset.ResetPassword(email, token, newPassword);

        public async Task<LoginResultDto> Login(string email, string password, string ipAddress, string? userAgent = null)
        {
            if (await _loginAttemptRepository.IsLockedOut(ipAddress))
                throw new LoginAttemptFailedException(email);

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Role.RolePermissions)
                .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null || !user.HasPassword || !AuthHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt, out bool needsRehash))
            {
                if (await _loginAttemptRepository.LoginAttemptFailed(ipAddress))
                    throw new LoginAttemptFailedException(email);
                if (user != null && !user.HasPassword)
                    throw new UnauthorizedException($"This account was created with {user.AuthProvider} sign in. Use the {user.AuthProvider} button, then set a password from your profile.");
                return null;
            }

            if (user.PartnerID == null)
                throw new NotPartnerException("not a partner account");

            if (needsRehash)
            {
                AuthHelper.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                await _context.SaveChangesAsync();
            }

            var claims = BuildPartnerClaims(user);

            return await IssueSession(user, claims, ipAddress, userAgent);
        }

        public async Task<LoginResultDto> ExternalLogin(ExternalUserInfoDto externalUser, AuthProviderEnum provider, string? ipAddress = null, string? userAgent = null)
        {
            if (provider != AuthProviderEnum.Google)
                throw new ValidationException($"Unsupported authentication provider {provider}");

            if (!externalUser.EmailVerified)
                throw new UnauthorizedException("Your Google email address is not verified");

            string email = externalUser.Email.ToLower();

            var user = await _context.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.GoogleId == externalUser.ProviderKey || u.Email == email);

            // Partner accounts are created by an administrator, never by a Google sign in.
            if (user == null || user.PartnerID == null)
                throw new NotPartnerException("not a partner account");

            if (user.GoogleId == null)
            {
                user.GoogleId = externalUser.ProviderKey;
                user.ExternalPictureUrl ??= externalUser.PictureUrl;
                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else if (user.GoogleId != externalUser.ProviderKey)
            {
                throw new UnauthorizedException("This email is already linked to a different Google account");
            }

            var claims = BuildPartnerClaims(user);

            return await IssueSession(user, claims, ipAddress, userAgent);
        }

        /// <summary>
        /// Same access / refresh pair as the customer portal — the refresh endpoint rebuilds
        /// partner claims from the account itself, so both portals share one session model.
        /// </summary>
        private async Task<LoginResultDto> IssueSession(User user, List<Claim> claims, string? ipAddress, string? userAgent)
        {
            if (user.IsDeleted || user.IsCurrentlySuspended)
                throw new UnauthorizedException("This account is unavailable or suspended");

            if (user.TwoFactorEnabled)
                return new LoginResultDto
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = _twoFactor.CreateChallengeToken(user),
                    Email = user.Email,
                    FullName = user.FullName
                };

            var accessToken = _jwtService.GenerateAccessToken(claims);
            var refreshToken = await _refreshTokenService.Issue(user.ID, ipAddress, userAgent);

            return new LoginResultDto
            {
                Token = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                UserId = user.ID,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}",
                TwoFactorEnrollmentRequired = _twoFactor.IsEnrollmentRequired(user)
            };
        }

        private List<Claim> BuildPartnerClaims(User user) => _claimsFactory.BuildPartnerClaims(user);

    }


}
