using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace VoltaXApi.Helpers;

public static class AuthHelper
{
  private const string AlphaNumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

  // Password storage format, encoded in the existing PasswordHash / PasswordSalt columns
  // so no schema change is needed:
  //   v2 (current): PasswordSalt = { 0x02 }, PasswordHash = ASP.NET Core Identity PasswordHasher
  //                 output (PBKDF2, salt and iteration count embedded in the hash).
  //   v1 (legacy):  PasswordSalt = 128 byte HMACSHA512 key, PasswordHash = HMACSHA512(password).
  // v1 hashes still verify and are re-hashed to v2 on the next successful sign in.
  private static readonly byte[] PasswordHashV2Marker = { 0x02 };
  private static readonly PasswordHasher<object> PasswordHasher = new();
  private static readonly object HasherUser = new();

  // Email verification code (typed by the user, so kept short; brute force is rate limited)
  public static string GenerateVerificationToken() => RandomNumberGenerator.GetString(AlphaNumeric, 6);

  public static string GeneratePhoneVerificationToken() => RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");

  public static string GenerateNumericCode(int digits = 6) =>
    RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, digits)).ToString("D" + digits);

  public static string GenerateRandomPassword() => RandomNumberGenerator.GetString(AlphaNumeric + "+*-_", 16);

  /// <summary>256 bit URL safe random token.</summary>
  public static string GenerateSecureToken(int bytes = 32) =>
    Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).Replace("+", "-").Replace("/", "_").TrimEnd('=');

  public static string Sha256Hex(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

  public static bool FixedTimeEqualsHex(string? storedHex, string candidateHex) =>
    storedHex != null && storedHex.Length == candidateHex.Length &&
    CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(storedHex.ToUpperInvariant()), Encoding.ASCII.GetBytes(candidateHex.ToUpperInvariant()));

  /// <summary>Keyed digest for short codes, which a plain hash would not protect against an offline guess.</summary>
  public static string KeyedDigest(string key, string purpose, string value) =>
    Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(purpose + ":" + value)));

  public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
  {
    passwordHash = Convert.FromBase64String(PasswordHasher.HashPassword(HasherUser, password));
    passwordSalt = (byte[])PasswordHashV2Marker.Clone();
  }

  public static bool VerifyPasswordHash(string password, byte[]? passwordHash, byte[]? passwordSalt) =>
    VerifyPasswordHash(password, passwordHash, passwordSalt, out _);

  /// <param name="needsRehash">True when the stored hash is legacy or uses outdated parameters.</param>
  public static bool VerifyPasswordHash(string password, byte[]? passwordHash, byte[]? passwordSalt, out bool needsRehash)
  {
    needsRehash = false;
    if (password == null || passwordHash == null || passwordSalt == null || passwordHash.Length == 0)
      return false;

    if (IsV2(passwordSalt))
    {
      var result = PasswordHasher.VerifyHashedPassword(HasherUser, Convert.ToBase64String(passwordHash), password);
      needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
      return result != PasswordVerificationResult.Failed;
    }

    using var hmac = new HMACSHA512(passwordSalt);
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    var valid = CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    needsRehash = valid;
    return valid;
  }

  private static bool IsV2(byte[] passwordSalt) => passwordSalt.Length == 1 && passwordSalt[0] == PasswordHashV2Marker[0];
}
