using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.ScaleOut;

namespace VoltaXApi.Services;

public record TwoFactorEnrollmentDto(string Secret, string OtpAuthUri, string QrCodeDataUri);
public record TwoFactorStatusDto(bool Eligible, bool Enabled, bool Required, int RecoveryCodesLeft);

public interface ITwoFactorService
{
    /// <summary>Admin and partner accounts can (and may be required to) use TOTP.</summary>
    bool IsEligible(User user);

    /// <summary>True when Auth:RequireTwoFactorForAdmins is on and this admin has not enrolled yet.</summary>
    bool IsEnrollmentRequired(User user);

    Task<TwoFactorStatusDto> GetStatus(int userID);
    Task<TwoFactorEnrollmentDto> BeginEnrollment(int userID);

    /// <summary>Turns 2FA on and returns the 10 recovery codes — the only time they exist in clear.</summary>
    Task<string[]> ConfirmEnrollment(int userID, string code);
    Task Disable(int userID, string? password, string code);

    /// <summary>Short lived (5 min), single purpose token handed out instead of a session.</summary>
    string CreateChallengeToken(User user);

    /// <summary>Checks the challenge token and the TOTP / recovery code; returns the user ID once.</summary>
    Task<int> VerifyChallenge(string challengeToken, string code);
}

/// <summary>
/// RFC 6238 TOTP (HMAC-SHA1, 30 s, 6 digits, ±1 step). The secret is stored protected with
/// ASP.NET Data Protection; recovery codes are stored as SHA-256 hashes.
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private const int RecoveryCodeCount = 10;
    private const int MaxChallengeAttempts = 5;
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly VoltaXApiDbContext _context;
    private readonly IConfiguration _config;
    private readonly ISecurityStateStore _cache;
    private readonly IDataProtector _secretProtector;
    private readonly ITimeLimitedDataProtector _challengeProtector;

    public TwoFactorService(VoltaXApiDbContext context, IConfiguration config, ISecurityStateStore cache, IDataProtectionProvider dataProtection)
    {
        _context = context;
        _config = config;
        _cache = cache;
        _secretProtector = dataProtection.CreateProtector("VoltaX.TwoFactor.Secret.v1");
        _challengeProtector = dataProtection.CreateProtector("VoltaX.TwoFactor.Challenge.v1").ToTimeLimitedDataProtector();
    }

    public bool IsEligible(User user) => user.PartnerID != null || user.Role?.Name is "Admin" or "Partner";

    public bool IsEnrollmentRequired(User user) =>
        !user.TwoFactorEnabled && user.Role?.Name == "Admin" && _config.GetValue("Auth:RequireTwoFactorForAdmins", false);

    public async Task<TwoFactorStatusDto> GetStatus(int userID)
    {
        var user = await LoadUser(userID);
        var left = string.IsNullOrEmpty(user.TwoFactorRecoveryCodes) ? 0 : user.TwoFactorRecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        return new TwoFactorStatusDto(IsEligible(user), user.TwoFactorEnabled, IsEnrollmentRequired(user), left);
    }

    public async Task<TwoFactorEnrollmentDto> BeginEnrollment(int userID)
    {
        var user = await LoadUser(userID);
        if (!IsEligible(user))
            throw new ValidationException("Two factor authentication is available for administrator and partner accounts only");
        if (user.TwoFactorEnabled)
            throw new ValidationException("Two factor authentication is already enabled. Disable it first to enroll a new device.");

        var secret = Totp.ToBase32(RandomNumberGenerator.GetBytes(20));
        user.TwoFactorSecret = _secretProtector.Protect(secret);
        user.TwoFactorLastUsedStep = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var issuer = _config["Auth:TwoFactorIssuer"] ?? "EVEON";
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}" +
                  $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        var png = PngByteQRCodeHelper.GetQRCode(uri, QRCodeGenerator.ECCLevel.Q, 5);

        return new TwoFactorEnrollmentDto(secret, uri, "data:image/png;base64," + Convert.ToBase64String(png));
    }

    public async Task<string[]> ConfirmEnrollment(int userID, string code)
    {
        var user = await LoadUser(userID);
        if (user.TwoFactorEnabled)
            throw new ValidationException("Two factor authentication is already enabled");
        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new ValidationException("Start the enrollment first");

        if (!VerifyTotp(user, code))
            throw new ValidationException("Invalid authentication code");

        var codes = Enumerable.Range(0, RecoveryCodeCount).Select(_ => NewRecoveryCode()).ToArray();
        user.TwoFactorRecoveryCodes = string.Join(';', codes.Select(HashRecoveryCode));
        user.TwoFactorEnabled = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return codes;
    }

    public async Task Disable(int userID, string? password, string code)
    {
        var user = await LoadUser(userID);
        if (!user.TwoFactorEnabled)
            throw new ValidationException("Two factor authentication is not enabled");

        if (user.HasPassword && !AuthHelper.VerifyPasswordHash(password ?? "", user.PasswordHash, user.PasswordSalt))
            throw new ValidationException("Incorrect password or authentication code");

        if (!VerifyTotp(user, code) && !ConsumeRecoveryCode(user, code))
            throw new ValidationException("Incorrect password or authentication code");

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorRecoveryCodes = null;
        user.TwoFactorLastUsedStep = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public string CreateChallengeToken(User user)
    {
        var nonce = AuthHelper.GenerateSecureToken(16);
        return _challengeProtector.Protect($"{user.ID}|{nonce}", ChallengeLifetime);
    }

    public async Task<int> VerifyChallenge(string challengeToken, string code)
    {
        const string invalid = "Invalid or expired authentication code";
        string payload;
        try { payload = _challengeProtector.Unprotect(challengeToken ?? ""); }
        catch (CryptographicException) { throw new UnauthorizedException("Your sign in expired, please sign in again"); }

        var parts = payload.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var userID))
            throw new UnauthorizedException(invalid);

        var nonce = parts[1];
        var attemptsKey = "2fa-attempts:" + nonce;
        var usedKey = "2fa-used:" + nonce;
        // Counted before checking, atomically across instances.
        var attempts = await _cache.IncrementAsync(attemptsKey, ChallengeLifetime);
        if (attempts > MaxChallengeAttempts || await _cache.ExistsAsync(usedKey))
            throw new UnauthorizedException("Too many attempts, please sign in again");

        var user = await LoadUser(userID);
        if (!user.TwoFactorEnabled || (!VerifyTotp(user, code) && !ConsumeRecoveryCode(user, code)))
            throw new UnauthorizedException(invalid);

        // Single use: of two concurrent successful verifications only one gets the session.
        if (!await _cache.TryAddAsync(usedKey, "1", ChallengeLifetime))
            throw new UnauthorizedException("Too many attempts, please sign in again");
        await _context.SaveChangesAsync();
        return user.ID;
    }

    private bool VerifyTotp(User user, string code)
    {
        if (string.IsNullOrEmpty(user.TwoFactorSecret) || code == null)
            return false;

        code = code.Replace(" ", "");
        if (code.Length != 6 || !code.All(char.IsDigit))
            return false;

        string secret;
        try { secret = _secretProtector.Unprotect(user.TwoFactorSecret); }
        catch (CryptographicException) { return false; }

        var step = Totp.Verify(Totp.FromBase32(secret), code, DateTimeOffset.UtcNow, user.TwoFactorLastUsedStep);
        if (step == null)
            return false;

        user.TwoFactorLastUsedStep = step;
        return true;
    }

    private static bool ConsumeRecoveryCode(User user, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrEmpty(user.TwoFactorRecoveryCodes))
            return false;

        var hash = HashRecoveryCode(code);
        var hashes = user.TwoFactorRecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        var match = hashes.FirstOrDefault(h => AuthHelper.FixedTimeEqualsHex(h, hash));
        if (match == null)
            return false;

        hashes.Remove(match);
        user.TwoFactorRecoveryCodes = string.Join(';', hashes);
        return true;
    }

    private static string NewRecoveryCode()
    {
        const string alphabet = "abcdefghjkmnpqrstuvwxyz23456789";
        return RandomNumberGenerator.GetString(alphabet, 5) + "-" + RandomNumberGenerator.GetString(alphabet, 5);
    }

    private static string HashRecoveryCode(string code) =>
        AuthHelper.Sha256Hex(code.Trim().Replace("-", "").Replace(" ", "").ToLowerInvariant());

    private async Task<User> LoadUser(int userID) =>
        await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.ID == userID)
        ?? throw new UnauthorizedException("Could not resolve the current user");
}

/// <summary>RFC 6238 / RFC 4226 one time passwords.</summary>
public static class Totp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int StepSeconds = 30;

    /// <summary>Returns the matched time step (window ±1), or null. Steps at or before <paramref name="lastUsedStep"/> are refused.</summary>
    public static long? Verify(byte[] key, string code, DateTimeOffset now, long? lastUsedStep)
    {
        var current = now.ToUnixTimeSeconds() / StepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            var step = current + offset;
            if (lastUsedStep != null && step <= lastUsedStep) continue;
            var expected = Encoding.ASCII.GetBytes(Compute(key, step));
            if (CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(code)))
                return step;
        }
        return null;
    }

    public static string Compute(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    public static string ToBase32(byte[] data)
    {
        var sb = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    public static byte[] FromBase32(string value)
    {
        var output = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in value.TrimEnd('=').ToUpperInvariant())
        {
            var index = Base32Alphabet.IndexOf(c);
            if (index < 0) throw new FormatException("Invalid base32 character");
            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return output.ToArray();
    }
}
