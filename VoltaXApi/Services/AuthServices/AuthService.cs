using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Helpers;
using System.Security.Claims;
using VoltaXApi.Exceptions.AuthExceptions;

namespace VoltaXApi.Services
{
  public class AuthService : IAuthService
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

    public AuthService(
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

    // Creating phone verification token and updating the user
    public async Task SendPhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto)
    {
      var user = await _userRepository.GetUserByEmail(addPhoneNumberDto.Email);
      user.Phone = addPhoneNumberDto.Phone;
      user.PhoneVerificationToken = AuthHelper.GeneratePhoneVerificationToken();
      user.IsPhoneNumberVerified = false;

      this._context.Set<User>().Entry(user).State = EntityState.Modified;
      await this._context.SaveChangesAsync();


      // Sending the phone verification token: 
      string smsMessage = "Your VoltaX verification code is " + user.PhoneVerificationToken + ". Enter this code in the app to verify your phone number. Do not share this code with anyone.";
      await this._snsService.SendSmsAsync(user.Phone, smsMessage);
    }

    // Reset Password Request
    public async Task ResetPasswordRequest(string email)
    {
      string verificationLink = await GetResetPasswordLinkForUserByEmail(email);
      string userName = (await this._userRepository.GetUserByEmail(email)).FullName;

      MailRequest requ = _mailRequestFactory.CreateResetPasswordMailRequest(email);

      await this._mailService.SendResetPasswordMailRequest(requ, userName, verificationLink);
    }

    // Reset Password Request For Mobile Application
    public async Task ResetPasswordRequestForMobile(string email)
    {
      var user = await _userRepository.GetUserByEmail(email);

      var code = new Random().Next(100000, 999999).ToString();
      var resetToken = await this._userRepository.GenerateResetPasswordTokenForUser(email);

      user.ResetPasswordCode = code;
      user.ResetPasswordCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
      user.ResetPasswordToken = resetToken;
      await _userRepository.Update(user);

      var userName = (await this._userRepository.GetUserByEmail(email)).FullName;

      MailRequest requ = _mailRequestFactory.CreateResetPasswordForMobileMailRequest(email);

      await this._mailService.SendResetPasswordForMobileMailRequest(requ, userName, code);
    }

    private async Task<string> GetResetPasswordLinkForUserByEmail(string email)
    {
      string spaLink = _config["SpaLink"];
      string resetToken = await this._userRepository.GenerateResetPasswordTokenForUser(email);
      string verificationLink = spaLink + "auth/reset-password?email=" + email + "&token=" + resetToken;
      return verificationLink;
    }

    // Creating phone verification token and updating the user
    public async Task CreateEmailVerificationToken(int userID)
    {
      var user = await _userRepository.GetByIdAsync(userID);
      user.EmailVerificationToken = AuthHelper.GenerateVerificationToken();
      user.IsEmailVerified = false;
      this._context.Set<User>().Entry(user).State = EntityState.Modified;
      await this._context.SaveChangesAsync();

      // Sending the verification email
      MailRequest requ = _mailRequestFactory.CreateVerificationMailRequest(user.Email);
      await this._mailService.SendVerificationCodeEmailAsync(requ, user.EmailVerificationToken, user.FullName);

    }

    public async Task<User> Register(UserForRegisterDto userForRegisterDto)
    {
      userForRegisterDto.Email = userForRegisterDto.Email.ToLower();
      string spaLink = _config["SpaLink"];

      if (await _userRepository.UserExists(userForRegisterDto.Email))
      {
        throw new ValidationException("Email already exists");
      }

      // Creating user
      byte[] passwordHash, passwordSalt;
      AuthHelper.CreatePasswordHash(userForRegisterDto.Password, out passwordHash, out passwordSalt);

      User user = await _userRepository.CreateUser(userForRegisterDto, passwordHash, passwordSalt);

      await _cardRepository.CreateCardForUser(user);

      MailRequest requ = _mailRequestFactory.CreateVerificationMailRequest(user.Email);

      string verificationLink = spaLink + "auth/verify-email?email=" + user.Email + "&token=" + user.EmailVerificationToken;
      // Sending Welcome Message
      var mailRequest = _mailRequestFactory.CreateWelcomeMailRequest(user.Email);
      await _mailService.SendWelcomeEmail(mailRequest, user.FirstName);
      await this._mailService.SendVerificationEmailAsync(requ, verificationLink, user.FullName);

      return user;
    }


    // Verifying the email
    public async Task<bool> VerifyEmail(string email, string token)
    {
      var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
      if (user == null || user.EmailVerificationToken != token)
        return false;



      user.IsEmailVerified = true;
      user.EmailVerificationToken = null; // clear the token
      await _context.SaveChangesAsync();

      return true;
    }

    // Verifying the phone number
    public async Task<string?> VerifyPhoneNumber(string email, string token)
    {
      var user = await _context.Users
        .Include(u => u.Role)
        .Include(u => u.Role.RolePermissions)
        .ThenInclude(up => up.Permission)
        .FirstOrDefaultAsync(x => x.Email == email);

      if (user == null || user.PhoneVerificationToken != token)
        return null;

      user.IsPhoneNumberVerified = true;
      user.PhoneVerificationToken = null;
      await _context.SaveChangesAsync();

      // Build updated claims
      var claims = BuildUserClaims(user);

      // Generate new JWT
      var newToken = _jwtService.GenerateToken(claims);

      return newToken;

    }

    public static void CreatePasswordHashStatic(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }



    public async Task<LoginResultDto> Login(string email, string password, string ipAddress)
    {
      var user = await _context.Users
        .Include(u => u.Role)
        .Include(u => u.Role.RolePermissions)
        .ThenInclude(up => up.Permission)
        .FirstOrDefaultAsync(x => x.Email == email);

      if (user.PartnerID != null)
      {
        throw new UnauthorizedException("A partner account should login from the partner portal");
      }

      try
      {
        var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);

        if (loginAttempt?.LockoutEndTime > DateTime.UtcNow)
          throw new LoginAttemptFailedException(email, loginAttempt.LockoutEndTime);

        if (user == null || !AuthHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
          await _loginAttemptRepository.LoginAttemptFailed(ipAddress);
          throw new UnauthorizedException("Email or Password incorrect");
        }
        var claims = BuildUserClaims(user);

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
        MailRequest mailRequest = _mailRequestFactory.CreateLoginFailedAttemptMailRequest(email);
        string resetPasswordLink = await GetResetPasswordLinkForUserByEmail(email);

        await _mailService.SendLoginAttemptFailedEmail(mailRequest, user.FullName, ipAddress, resetPasswordLink);

        throw;
      }
    }

    private List<Claim> BuildUserClaims(User user)
    {
      var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim("emailVerified", user.IsEmailVerified.ToString()),
            new Claim("phoneVerified", user.IsPhoneNumberVerified.ToString()),
        };

      foreach (var userPermission in user.Role.RolePermissions)
      {
        claims.Add(new Claim("permission", userPermission.Permission.Name));
        claims.Add(new Claim($"permission_scope:{userPermission.Permission.Name}", userPermission.Scope.ToString()));
      }

      return claims;
    }



    public async Task ChangePasswordAsync(UserPasswordChangeDto userPasswordChangeDto)
    {
      var user = await _userRepository.GetUser(userPasswordChangeDto.ID);

      if (user == null)
      {
        throw new UserNotFoundException("User not found.");
      }

      if (!AuthHelper.VerifyPasswordHash(userPasswordChangeDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
      {
        throw new IncorrectPasswordException("The current password is incorrect.");
      }

      AuthHelper.CreatePasswordHash(userPasswordChangeDto.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);

      user.PasswordSalt = passwordSalt;
      user.PasswordHash = passwordHash;

      await _context.SaveChangesAsync();

      // Sending the email of the changed password
      MailRequest requ = _mailRequestFactory.CreateChangedPasswordMailRequest(user.Email);
      await this._mailService.SendPasswordChangedMail(requ, user.FirstName);
    }

    public async Task<string> VerifyResetPasswordCodeForMobile(UserResetPasswordForMobileDto userResetPasswordForMobileDto)
    {
      // Find user by email or phone
      var user = await _userRepository.GetUserByEmail(userResetPasswordForMobileDto.Email);

      if (user == null)
      {
        throw new UserNotFoundException("User not found with the provided email or phone number.");
      }

      // Check if user has a reset password request
      if (string.IsNullOrEmpty(user.ResetPasswordCode))
      {
        throw new ResetPasswordCodeNotFoundException();
      }

      // Check if code has expired
      if (user.ResetPasswordCodeExpiresAt.HasValue && user.ResetPasswordCodeExpiresAt.Value < DateTime.UtcNow)
      {
        throw new ExpiredResetPasswordCodeException(user.ResetPasswordCodeExpiresAt.Value);
      }

      // Verify the code
      if (user.ResetPasswordCode != userResetPasswordForMobileDto.Code)
      {
        throw new InvalidResetPasswordCodeException();
      }

      return user.ResetPasswordToken;
    }

  }


}