using System.Security.Cryptography;
using System.Text;

namespace VoltaXApi.Ocpi.Services
{
    // Credentials tokens: generation, hashing and the "Authorization: Token ..." header format.
    public static class OcpiTokens
    {
        public static string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(30);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

        // OCPI 2.2.1 sends the token Base64 encoded; older peers send it raw. Both are tried.
        public static IReadOnlyList<string> FromHeader(string? header)
        {
            if (string.IsNullOrWhiteSpace(header)) return Array.Empty<string>();
            var value = header.Trim();
            if (!value.StartsWith("Token ", StringComparison.OrdinalIgnoreCase)) return Array.Empty<string>();
            value = value[6..].Trim();
            if (value.Length == 0 || value.Length > 512) return Array.Empty<string>();
            var candidates = new List<string>();
            var decoded = TryDecodeBase64(value);
            if (decoded != null) candidates.Add(decoded);
            candidates.Add(value);
            return candidates;
        }

        public static string ToHeader(string token) => "Token " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

        private static string? TryDecodeBase64(string value)
        {
            var buffer = new byte[value.Length];
            if (!Convert.TryFromBase64String(value, buffer, out var written)) return null;
            try
            {
                var text = new UTF8Encoding(false, true).GetString(buffer, 0, written);
                return text.Length > 0 && text.All(c => c >= 0x21 && c <= 0x7e) ? text : null;
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }
    }
}
