using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Helpers;

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

        public PartnerAuthService(
          IUserRepository userRepository,
          IMailService mailService,
          IConfiguration config,
          VoltaXApiDbContext context,
          ILoginAttemptRepository loginAttemptRepository,
          IMailRequestFactory mailRequestFactory,
          ICardRepository cardRepository,
          ISnsService snsService
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
        
        public async Task<User> Login(string email, string password, string ipAddress)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(x => x.Email == email);
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
                return user;
            }
            catch (LoginAttemptFailedException ex)
            {
                MailRequest mailRequest = _mailRequestFactory.CreateLoginFailedAttemptMailRequest(email);
                //string resetPasswordLink = await GetResetPasswordLinkForUserByEmail(email);

                //await _mailService.SendLoginAttemptFailedEmail(mailRequest, user.FullName, ipAddress, resetPasswordLink);

                throw; 
            }
        
        }

    }


}