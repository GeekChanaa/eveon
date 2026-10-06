using System.Security.Cryptography;

namespace VoltaXApi.Helpers
{
    public static class RandomString
    {
        public static string RandString(int length = 64)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return RandomNumberGenerator.GetString(chars, length);
        }
    }
}
