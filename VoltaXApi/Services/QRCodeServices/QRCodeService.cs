using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using QRCoder;

namespace VoltaXApi.Services
{
    public class QRCodeService : IQRCodeService
    {
        private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 20;

        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QRCodeService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// A new QR value for a charge point: {base}/charge/{20 random characters}. The base is
        /// AppSettings:QrBaseUrl when set, otherwise the address this request reached the API on
        /// (https://api-v2.eveon.ma behind Caddy, http://localhost:8000 in development).
        /// </summary>
        public string NewChargePointQrValue()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(CodeLength);
            var code = new char[CodeLength];
            for (int i = 0; i < CodeLength; i++)
                code[i] = AllowedCharacters[randomBytes[i] % AllowedCharacters.Length];
            return $"{BaseUrl()}/charge/{new string(code)}";
        }

        /// <summary>
        /// Extracts the unique code from a QR value URL
        /// Example: "https://api-v2.eveon.ma/charge/ABC123" returns "ABC123"
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

        private string BaseUrl()
        {
            var configured = _configuration["AppSettings:QrBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null || !request.Host.HasValue)
                throw new InvalidOperationException("AppSettings:QrBaseUrl is not configured and there is no request to derive it from.");
            return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
        }
    }
}
