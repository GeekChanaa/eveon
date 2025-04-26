using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Services
{
  public class AuthService : IAuthService
  {
    private readonly IUserRepository _userRepository;
    private readonly IMailService _mailService;
    private readonly VoltaXApiDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(
      IUserRepository userRepository,
      IMailService mailService,
      IConfiguration config,
      VoltaXApiDbContext context
    )
    {
      _userRepository = userRepository;
      _mailService = mailService;
      _context = context;
      _config = config;
    }

    // Creating phone verification token and updating the user
    public async Task CreatePhoneVerificationToken(AddPhoneNumberDto addPhoneNumberDto)
    {
      var user = await _userRepository.GetUserByEmail(addPhoneNumberDto.Email);
      user.Phone = addPhoneNumberDto.Phone;
      user.PhoneVerificationToken = this.GenerateVerificationToken();
      user.IsPhoneNumberVerified = false;

      this._context.Set<User>().Entry(user).State = EntityState.Modified;
      await this._context.SaveChangesAsync();
    }

    // Creating phone verification token and updating the user
    public async Task CreateEmailVerificationToken(int userID)
    {
      var user = await _userRepository.GetByIdAsync(userID);
      user.EmailVerificationToken = this.GenerateVerificationToken();
      user.IsEmailVerified = false;

      this._context.Set<User>().Entry(user).State = EntityState.Modified;
      await this._context.SaveChangesAsync();
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
      var user = new User
      {
        Email = userForRegisterDto.Email,
        FirstName = userForRegisterDto.FirstName,
        LastName = userForRegisterDto.LastName,
        Phone = userForRegisterDto.Phone,
      };

      byte[] passwordHash, passwordSalt;
      CreatePasswordHash(userForRegisterDto.Password, out passwordHash, out passwordSalt);
      user.PasswordHash = passwordHash;
      user.PasswordSalt = passwordSalt;

      user.IsEmailVerified = false;
      user.EmailVerificationToken = GenerateVerificationToken();

      await _context.Users.AddAsync(user);
      await _context.SaveChangesAsync();

      Card userCard = new Card
      {
        CardNumber = GenerateCardNumber(),
        CardType = CardTypeEnum.Standard,
        ExpirationDate = DateTime.Now.AddYears(2),
        MaxCount = 1,
        Status = CardStatusEnum.Inactive,
        Balance = 100,
        Note = "Initial card",
        UserID = user.ID
      };

      _context.Cards.Add(userCard);


      MailRequest requ = new MailRequest
      {
        Phone = "",
        Email = "support@voltaxcharging.com",
        Name = "CHANAA mohammed",
        ToEmails = new List<string>() { user.Email },
        Subject = "Email Verification",
        Body = ""
      };
      string verificationLink = spaLink + "auth/verify-email?email=" + user.Email + "&token=" + user.EmailVerificationToken;
      await this._mailService.SendVerificationEmailAsync(requ, verificationLink);

      await _context.SaveChangesAsync();

      return user;
    }

    private string GenerateCardNumber()
    {
      Guid guid = Guid.NewGuid();

      string cardNumber = guid.ToString().Replace("-", "");
      return cardNumber.Substring(0, 16);
    }


    // Generating a verification token 
    private string GenerateVerificationToken()
    {
      Random random = new Random();
      const int tokenLength = 6;
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // only digits
      return new string(Enumerable.Repeat(chars, tokenLength)
          .Select(s => s[random.Next(s.Length)]).ToArray());
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
    public async Task<bool> VerifyPhoneNumber(string email, string token)
    {
      var user = await _userRepository.GetUserByEmail(email);
      if (user == null || user.PhoneVerificationToken != token)
        return false;

      user.IsPhoneNumberVerified = true;
      user.PhoneVerificationToken = null;
      await _context.SaveChangesAsync();

      return true;
    }

    public static void CreatePasswordHashStatic(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }

    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512())
      {
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
      }
    }

    public async Task<User> Login(string email, string password, string ipAddress)
    {
      var loginAttempt = await _context.LoginAttempts.FirstOrDefaultAsync(x => x.IpAddress == ipAddress);

      if (loginAttempt?.LockoutEndTime > DateTime.UtcNow)
      {
        throw new Exception("Too many failed attempts");
      }
      var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
      if (user == null)
      {
        if (loginAttempt == null)
        {
          loginAttempt = new LoginAttempt { IpAddress = ipAddress, FailedAttempts = 1 };
          _context.LoginAttempts.Add(loginAttempt);
          await _context.SaveChangesAsync();
        }
        else
        {
          loginAttempt.FailedAttempts++;
          if (loginAttempt.FailedAttempts >= 5)
          {
            loginAttempt.LockoutEndTime = DateTime.UtcNow.AddMinutes(5);
          }
          await _context.SaveChangesAsync();
        }
        return null;
      }
      if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        return null;
      return user;
    }

    public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
      using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
      {
        var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        for (int i = 0; i < computedHash.Length; i++)
        {
          if (computedHash[i] != passwordHash[i]) return false;
        }
      }
      return true;
    }

    public async Task ChangePasswordAsync(UserPasswordChangeDto userPasswordChangeDto)
    {
      var user = await _userRepository.GetUser(userPasswordChangeDto.ID);

      if (user == null)
      {
        throw new UserNotFoundException("User not found.");
      }

      if (!VerifyPasswordHash(userPasswordChangeDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
      {
        throw new IncorrectPasswordException("The current password is incorrect.");
      }

      CreatePasswordHash(userPasswordChangeDto.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);

      user.PasswordSalt = passwordSalt;
      user.PasswordHash = passwordHash;

      await _context.SaveChangesAsync();
    }




  }


}