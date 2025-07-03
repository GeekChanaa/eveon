
namespace VoltaXApi.Helpers;

public static class AuthHelper
{
  // Generating a verification token 
  public static string GenerateVerificationToken()
  {
    Random random = new Random();
    const int tokenLength = 6;
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    return new string(Enumerable.Repeat(chars, tokenLength)
        .Select(s => s[random.Next(s.Length)]).ToArray());
  }

  public static string GeneratePhoneVerificationToken()
  {
    Random random = new Random();
    const int tokenLength = 6;
    const string chars = "0123456789";
    return new string(Enumerable.Repeat(chars, tokenLength)
        .Select(s => s[random.Next(s.Length)]).ToArray());
  }

  // Generate Random Password
  public static string GenerateRandomPassword()
  {
    Random random = new Random();
    const int tokenLength = 12;
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+*-_";
    return new string(Enumerable.Repeat(chars, tokenLength)
        .Select(s => s[random.Next(s.Length)]).ToArray());
  }

  public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
  {
    using (var hmac = new System.Security.Cryptography.HMACSHA512())
    {
      passwordSalt = hmac.Key;
      passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    }
  }
    
  public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
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
}