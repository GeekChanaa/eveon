using System;
using System.Security.Cryptography;
using System.Text;
namespace VoltaXApi.Helpers
{
    public class TokenGenerator
{
    public static string GenerateToken()
    {
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            byte[] tokenData = new byte[32]; // Length of the token
            rng.GetBytes(tokenData);

            return Convert.ToBase64String(tokenData);
        }
    }
}
}

