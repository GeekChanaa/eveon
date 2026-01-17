using System;
using System.Linq;
using System.Security.Cryptography;
using QRCoder;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    public class QRCodeService : IQRCodeService
    {
        private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string BaseUrl = "https://app.voltax.com";
        
        /// <summary>
        /// Generates a complete deep link URL for a charge point
        /// Format: https://app.voltax.com/charge/{uniqueCode}
        /// This URL will open the VoltaX mobile app if installed, otherwise opens the website
        /// </summary>
        public static string GenerateQRCodeValueForChargePoint(ChargePoint chargePoint)
        {
            const int codeLength = 20;
            var randomBytes = new byte[codeLength];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var result = new char[codeLength];
            for (int i = 0; i < codeLength; i++)
            {
                result[i] = AllowedCharacters[randomBytes[i] % AllowedCharacters.Length];
            }

            var uniqueCode = new string(result);
            
            // Return Universal/App Link format for mobile apps
            return $"{BaseUrl}/charge/{uniqueCode}";
        }
        
        /// <summary>
        /// Generates a QR code value with custom base URL
        /// </summary>
        public static string GenerateQRCodeValueWithCustomUrl(string baseUrl = BaseUrl)
        {
            const int codeLength = 20;
            var randomBytes = new byte[codeLength];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var result = new char[codeLength];
            for (int i = 0; i < codeLength; i++)
            {
                result[i] = AllowedCharacters[randomBytes[i] % AllowedCharacters.Length];
            }

            var uniqueCode = new string(result);
            return $"{baseUrl}/charge/{uniqueCode}";
        }
        
        /// <summary>
        /// Extracts the unique code from a QR value URL
        /// Example: "https://app.voltax.com/charge/ABC123" returns "ABC123"
        /// </summary>
        public static string ExtractCodeFromQRValue(string qrValue)
        {
            if (string.IsNullOrEmpty(qrValue))
                return string.Empty;
                
            var parts = qrValue.Split('/');
            return parts.Length > 0 ? parts[^1] : qrValue;
        }

        public byte[] GenerateQr(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(20);
        }
    }
}
