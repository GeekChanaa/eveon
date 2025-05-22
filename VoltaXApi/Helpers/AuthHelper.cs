
namespace VoltaXApi.Helpers;

public static class AuthHelper
{
    // Generating a verification token 
    public static string GenerateVerificationToken()
    {
      Random random = new Random();
      const int tokenLength = 6;
      const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // only digits
      return new string(Enumerable.Repeat(chars, tokenLength)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}