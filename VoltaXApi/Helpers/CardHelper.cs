using System;
using System.Linq;

namespace VoltaXApi.Helpers
{
    public static class CardHelper
    {
        private static readonly Random random = new();

        public static string GenerateRandomCardNumber(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var cardNumber = new char[length];

            for (int i = 0; i < length; i++)
                cardNumber[i] = chars[random.Next(chars.Length)];

            return new string(cardNumber);
        }
    }
}