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

        public PartnerAuthService(
          IUserRepository userRepository,
          IMailService mailService,
          IConfiguration config,
          VoltaXApiDbContext context,
          ILoginAttemptRepository loginAttemptRepository,
          IMailRequestFactory mailRequestFactory,
          ICardRepository cardRepository,
          ISnsService snsService,
          IJwtService jwtService
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

        public async Task<LoginResultDto> Login(string email, string password, string ipAddress)
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

                if (user == null || !AuthHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    await _loginAttemptRepository.LoginAttemptFailed(ipAddress);
                    return null;
                }
                var claims = BuildPartnerClaims(user);

                var token = _jwtService.GenerateToken(claims);
                return new LoginResultDto
                {
                    Token = token,
                    UserId = user.ID,
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}"
                };
            }
            catch (LoginAttemptFailedException ex)
            {
                // MailRequest mailRequest = _mailRequestFactory.CreateLoginFailedAttemptMailRequest(email);
                // string resetPasswordLink = await GetResetPasswordLinkForUserByEmail(email);

                // await _mailService.SendLoginAttemptFailedEmail(mailRequest, user.FullName, ipAddress, resetPasswordLink);

                throw;
            }
        }

        private List<Claim> BuildPartnerClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("partnerID",  user.PartnerID?.ToString() ?? ""),
            };

            foreach (var userPermission in user.Role.RolePermissions)
            {
                claims.Add(new Claim("permission", userPermission.Permission.Name));
                claims.Add(new Claim($"permission_scope:{userPermission.Permission.Name}", userPermission.Scope.ToString()));
            }

            return claims;
        }

    }


}