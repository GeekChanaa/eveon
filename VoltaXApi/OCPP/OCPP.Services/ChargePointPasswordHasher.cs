using System.Security.Cryptography;
using System.Text;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Salted PBKDF2-SHA256 hashes for charger Basic auth passwords (OCPP security profile 1/2).
    /// Stored format: PBKDF2-SHA256$iterations$salt(base64)$hash(base64). Anything else is a legacy plain-text value.
    /// </summary>
    public static class ChargePointPasswordHasher
    {
        private const string Prefix = "PBKDF2-SHA256$";
        private const int Iterations = 210_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        public const int MinLength = 16;
        public const int MaxLength = 40;
        private const string GeneratedAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        public static bool IsHashed(string? stored) => stored != null && stored.StartsWith(Prefix, StringComparison.Ordinal);

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>Constant-time verification. needsRehash is true for legacy plain-text values that matched.</summary>
        public static bool Verify(string? stored, string provided, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(stored) || provided == null) return false;

            if (!IsHashed(stored))
            {
                var match = CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(Encoding.UTF8.GetBytes(stored)), SHA256.HashData(Encoding.UTF8.GetBytes(provided)));
                needsRehash = match;
                return match;
            }

            var parts = stored.Substring(Prefix.Length).Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0) return false;
            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }
            var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(provided), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
            needsRehash = ok && iterations < Iterations;
            return ok;
        }

        /// <summary>OCPP passwordString: 16-40 printable ASCII characters.</summary>
        public static bool IsValidPassword(string? password) =>
            password != null && password.Length >= MinLength && password.Length <= MaxLength && password.All(c => c >= 0x20 && c <= 0x7E);

        public static string Generate(int length = 32)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = GeneratedAlphabet[RandomNumberGenerator.GetInt32(GeneratedAlphabet.Length)];
            return new string(chars);
        }
    }
}
