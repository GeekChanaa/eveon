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
          IUserClaimsFactory claimsFactory
        )
        {
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

        public async Task PartnerResetPasswordRequest(string email)
        {
            User user = await this._userRepository.GetUserByEmail(email);

            MailRequest requ = _mailRequestFactory.CreatePasswordResetPasswordMailRequest(email);
            string newPassword = AuthHelper.GenerateRandomPassword();

            byte[] passwordHash, passwordSalt;
            AuthHelper.CreatePasswordHash(newPassword, out passwordHash, out passwordSalt);
            user.PasswordSalt = passwordSalt;
            user.PasswordHash = passwordHash;
            await _context.SaveChangesAsync();

            await this._mailService.SendPartnerResetPasswordMailRequest(requ, user.FullName, newPassword);
        }

        public async Task<LoginResultDto> Login(string email, string password, string ipAddress, string? userAgent = null)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Role.RolePermissions)
                .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user.PartnerID == null)
                throw new NotPartnerException("not a partner account");

            try
            {
                var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);

                if (loginAttempt?.LockoutEndTime > DateTime.UtcNow)
                    throw new LoginAttemptFailedException(email, loginAttempt.LockoutEndTime);

                if (!user.HasPassword)
                    throw new UnauthorizedException($"This account was created with {user.AuthProvider} sign in. Use the {user.AuthProvider} button, then set a password from your profile.");

                if (user == null || !AuthHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    await _loginAttemptRepository.LoginAttemptFailed(ipAddress);
                    return null;
                }
                var claims = BuildPartnerClaims(user);

                return await IssueSession(user, claims, ipAddress, userAgent);
            }
            catch (LoginAttemptFailedException ex)
            {
                // MailRequest mailRequest = _mailRequestFactory.CreateLoginFailedAttemptMailRequest(email);
                // string resetPasswordLink = await GetResetPasswordLinkForUserByEmail(email);

                // await _mailService.SendLoginAttemptFailedEmail(mailRequest, user.FullName, ipAddress, resetPasswordLink);

                throw;
            }
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
            if (user.IsDeleted || user.SuspendedAt != null)
                throw new UnauthorizedException("This account is unavailable or suspended");
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
                FullName = $"{user.FirstName} {user.LastName}"
            };
        }

        private List<Claim> BuildPartnerClaims(User user) => _claimsFactory.BuildPartnerClaims(user);

    }


}
