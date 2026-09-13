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
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserClaimsFactory _claimsFactory;

    public AuthService(
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

    // Creating phone verification token and updating the user
    public async Task SendPhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto)
    {
      var user = await _userRepository.GetUserByEmail(addPhoneNumberDto.Email);

      if (await _userRepository.PhoneExists(addPhoneNumberDto.Phone, user.ID))
      {
        throw new ValidationException("This phone number is already used by another account");
      }

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

      if (await _userRepository.PhoneExists(userForRegisterDto.Phone))
      {
        throw new ValidationException("Phone number already exists");
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



    public async Task<LoginResultDto> Login(string identifier, string password, string ipAddress, string? userAgent = null)
    {
      var user = await FindUserForLogin(identifier);

      if (user == null)
      {
        await _loginAttemptRepository.LoginAttemptFailed(ipAddress);
        throw new UnauthorizedException("Email, phone number or password incorrect");
      }

      if (user.PartnerID != null)
      {
        throw new UnauthorizedException("A partner account should login from the partner portal");
      }

      try
      {
        var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);

        if (loginAttempt?.LockoutEndTime > DateTime.UtcNow)
          throw new LoginAttemptFailedException(user.Email, loginAttempt.LockoutEndTime);

        if (user != null && !user.HasPassword)
        {
          throw new UnauthorizedException($"This account was created with {user.AuthProvider} sign in. Use the {user.AuthProvider} button, then set a password from your profile.");
        }

        if (!AuthHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
          await _loginAttemptRepository.LoginAttemptFailed(ipAddress);
          throw new UnauthorizedException("Email, phone number or password incorrect");
        }
        var claims = BuildUserClaims(user);

        return await IssueSession(user, claims, ipAddress, userAgent);
      }
      catch (LoginAttemptFailedException ex)
      {
        MailRequest mailRequest = _mailRequestFactory.CreateLoginFailedAttemptMailRequest(user.Email);
        string resetPasswordLink = await GetResetPasswordLinkForUserByEmail(user.Email);

        await _mailService.SendLoginAttemptFailedEmail(mailRequest, user.FullName, ipAddress, resetPasswordLink);

        throw;
      }
    }

    /// <summary>
    /// The login form takes a single field, so the identifier is either an email address or
    /// a phone number in any of the shapes <see cref="PhoneHelper"/> accepts.
    /// </summary>
    private async Task<User?> FindUserForLogin(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      if (PhoneHelper.LooksLikePhoneNumber(identifier))
      {
        string phone = PhoneHelper.Normalize(identifier);

        // Phone numbers are not unique in the database, so a verified owner wins over an
        // account that merely typed the number in without ever confirming it.
        return await _context.Users
          .Include(u => u.Role)
          .ThenInclude(r => r.RolePermissions)
          .ThenInclude(rp => rp.Permission)
          .OrderByDescending(u => u.IsPhoneNumberVerified)
          .FirstOrDefaultAsync(u => u.Phone == phone);
      }

      string email = identifier.Trim().ToLower();
      return await GetUserWithClaimsData(u => u.Email == email);
    }

    private List<Claim> BuildUserClaims(User user) => _claimsFactory.BuildUserClaims(user);



    public async Task ChangePasswordAsync(UserPasswordChangeDto userPasswordChangeDto)
    {
      var user = await _userRepository.GetUser(userPasswordChangeDto.ID);

      if (user == null)
      {
        throw new UserNotFoundException("User not found.");
      }

      if (!user.HasPassword)
      {
        throw new ValidationException("This account has no password yet. Use the reset password flow to set one.");
      }

      if (!AuthHelper.VerifyPasswordHash(userPasswordChangeDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
      {
        throw new IncorrectPasswordException("The current password is incorrect.");
      }

      AuthHelper.CreatePasswordHash(userPasswordChangeDto.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);

      user.PasswordSalt = passwordSalt;
      user.PasswordHash = passwordHash;

      await _context.SaveChangesAsync();

      // Someone who knew the old password (or a stolen token) must not keep a live session.
      await _refreshTokenService.RevokeAllForUser(user.ID, "password-change");

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

    // ---------------------------------------------------------------------
    // External (OAuth) sign in
    // ---------------------------------------------------------------------

    public async Task<LoginResultDto> ExternalLogin(ExternalUserInfoDto externalUser, AuthProviderEnum provider, string? ipAddress = null, string? userAgent = null)
    {
      if (provider != AuthProviderEnum.Google)
        throw new ValidationException($"Unsupported authentication provider {provider}");

      if (!externalUser.EmailVerified)
        throw new UnauthorizedException("Your Google email address is not verified");

      string email = externalUser.Email.ToLower();

      var user = await GetUserWithClaimsData(u => u.GoogleId == externalUser.ProviderKey)
                 ?? await GetUserWithClaimsData(u => u.Email == email);

      if (user == null)
      {
        user = await CreateUserFromExternalIdentity(externalUser, provider, email);
      }
      else
      {
        if (user.PartnerID != null)
          throw new UnauthorizedException("A partner account should login from the partner portal");

        // First Google sign in on an account that was created with a password: link the two.
        if (user.GoogleId == null)
        {
          user.GoogleId = externalUser.ProviderKey;
          user.ExternalPictureUrl ??= externalUser.PictureUrl;
          // Google already proved ownership of the address
          user.IsEmailVerified = true;
          user.EmailVerificationToken = null;
          user.UpdatedAt = DateTime.UtcNow;
          await _context.SaveChangesAsync();
        }
        else if (user.GoogleId != externalUser.ProviderKey)
        {
          throw new UnauthorizedException("This email is already linked to a different Google account");
        }
      }

      var claims = BuildUserClaims(user);

      return await IssueSession(user, claims, ipAddress, userAgent);
    }

    public async Task LinkExternalAccount(int userID, ExternalUserInfoDto externalUser, AuthProviderEnum provider)
    {
      if (provider != AuthProviderEnum.Google)
        throw new ValidationException($"Unsupported authentication provider {provider}");

      var user = await _userRepository.GetUser(userID);
      if (user == null)
        throw new UserNotFoundException("User not found.");

      if (user.GoogleId != null && user.GoogleId != externalUser.ProviderKey)
        throw new ValidationException("Another Google account is already linked to this profile");

      var alreadyTaken = await _context.Users
        .AnyAsync(u => u.GoogleId == externalUser.ProviderKey && u.ID != userID);

      if (alreadyTaken)
        throw new ValidationException("This Google account is already linked to another user");

      user.GoogleId = externalUser.ProviderKey;
      user.ExternalPictureUrl ??= externalUser.PictureUrl;
      user.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();
    }

    public async Task UnlinkExternalAccount(int userID, AuthProviderEnum provider)
    {
      if (provider != AuthProviderEnum.Google)
        throw new ValidationException($"Unsupported authentication provider {provider}");

      var user = await _userRepository.GetUser(userID);
      if (user == null)
        throw new UserNotFoundException("User not found.");

      if (user.GoogleId == null)
        throw new ValidationException("No Google account is linked to this profile");

      // Removing the only credential would lock the account out for good.
      if (!user.HasPassword)
        throw new ValidationException("Set a password before removing Google sign in, otherwise you would lock yourself out.");

      user.GoogleId = null;
      user.AuthProvider = AuthProviderEnum.Local;
      user.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();
    }

    public async Task<LinkedAccountsDto> GetLinkedAccounts(int userID)
    {
      var user = await _userRepository.GetUser(userID);
      if (user == null)
        throw new UserNotFoundException("User not found.");

      return new LinkedAccountsDto
      {
        GoogleLinked = user.GoogleId != null,
        GoogleEmail = user.GoogleId != null ? user.Email : null,
        HasPassword = user.HasPassword,
        AuthProvider = user.AuthProvider.ToString()
      };
    }

    private async Task<User> CreateUserFromExternalIdentity(ExternalUserInfoDto externalUser, AuthProviderEnum provider, string email)
    {
      var user = new User
      {
        Email = email,
        FirstName = string.IsNullOrWhiteSpace(externalUser.FirstName) ? email.Split('@')[0] : externalUser.FirstName,
        LastName = externalUser.LastName ?? string.Empty,
        GoogleId = externalUser.ProviderKey,
        AuthProvider = provider,
        ExternalPictureUrl = externalUser.PictureUrl,
        IsEmailVerified = true,
        RoleID = 2,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      await _context.Users.AddAsync(user);
      await _context.SaveChangesAsync();

      await _cardRepository.CreateCardForUser(user);

      var mailRequest = _mailRequestFactory.CreateWelcomeMailRequest(user.Email);
      await _mailService.SendWelcomeEmail(mailRequest, user.FirstName);

      // Re-read so Role / RolePermissions are loaded for the claims
      return await GetUserWithClaimsData(u => u.ID == user.ID);
    }

    /// <summary>
    /// Mints the access / refresh pair handed back by every sign in path, so the shape of
    /// a session never depends on how the user got in.
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

    private Task<User?> GetUserWithClaimsData(System.Linq.Expressions.Expression<Func<User, bool>> predicate)
    {
      return _context.Users
        .Include(u => u.Role)
        .ThenInclude(r => r.RolePermissions)
        .ThenInclude(rp => rp.Permission)
        .FirstOrDefaultAsync(predicate);
    }

  }


}
