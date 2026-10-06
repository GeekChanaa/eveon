using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Services;
public sealed class PhoneLoginRateLimitException : Exception { }
public partial class AuthService
{
    private byte[] PhoneCodeDigest(string salt, string code)
    {
        var secret = _config["AppSettings:Token"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Authentication signing configuration is missing.");
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes("phone-login:" + salt + ":" + code));
    }
    public static string NormalizeMobilePhone(string input)
    {
        if (input == null || !Regex.IsMatch(input, @"^[+0-9\s().-]+$")) throw new ArgumentException("Invalid Moroccan mobile number.");
        var digits = Regex.Replace(input, @"[\s().-]", "");
        if (digits.StartsWith("00212")) digits = "+212" + digits[5..];
        else if (digits.StartsWith("212")) digits = "+" + digits;
        else if (digits.StartsWith("0")) digits = "+212" + digits[1..];
        else if (!digits.StartsWith("+")) digits = "+212" + digits;
        if (!Regex.IsMatch(digits, @"^\+212[67][0-9]{8}$")) throw new ArgumentException("Invalid Moroccan mobile number.");
        return digits;
    }

    public async Task<object> RequestPhoneLogin(string number, string ip)
    {
        var phone = NormalizeMobilePhone(number);
        var now = DateTime.UtcNow;
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var recentSends = await _context.PhoneLoginChallenges.Where(c => c.IpAddress == ip && c.WindowStart > now.AddHours(-1)).SumAsync(c => c.Sends);
        var challenge = await _context.PhoneLoginChallenges.SingleOrDefaultAsync(c => c.Phone == phone);
        if (recentSends >= 20 || challenge != null && (challenge.ResendAt > now || challenge.WindowStart > now.AddHours(-1) && challenge.Sends >= 5))
            throw new PhoneLoginRateLimitException();
        if (challenge == null) { challenge = new PhoneLoginChallenge { Phone = phone, WindowStart = now }; _context.Add(challenge); }
        if (challenge.WindowStart <= now.AddHours(-1)) { challenge.WindowStart = now; challenge.Sends = 0; }
        var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        challenge.Salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        challenge.Digest = Convert.ToHexString(PhoneCodeDigest(challenge.Salt, code));
        challenge.ChallengeId = Guid.NewGuid().ToString("N");
        challenge.ExpiresAt = now.AddMinutes(5); challenge.ResendAt = now.AddSeconds(60);
        challenge.IpAddress = ip; challenge.Sends++; challenge.Attempts = 0; challenge.Consumed = false;
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
        // Login only to a single previously verified phone owner; no silent account creation/linking.
        var owners = await _context.Users.CountAsync(u => u.Phone == phone && u.IsPhoneNumberVerified && !u.IsDeleted && (u.SuspendedAt == null || u.SuspendedAt <= DateTime.UtcNow) && u.PartnerID == null);
        if (owners == 1) await _snsService.SendSmsAsync(phone, $"Your VoltaX login code is {code}. It expires in 5 minutes. Do not share it.");
        return new { challengeId = challenge.ChallengeId, expiresAt = challenge.ExpiresAt, resendAt = challenge.ResendAt };
    }

    public async Task<LoginResultDto?> VerifyPhoneLogin(string number, string challengeId, string code, string ip, string? userAgent)
    {
        var phone = NormalizeMobilePhone(number);
        if (!Regex.IsMatch(code ?? "", @"^[0-9]{6}$")) return null;
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var challenge = await _context.PhoneLoginChallenges.SingleOrDefaultAsync(c => c.Phone == phone && c.ChallengeId == challengeId);
        if (challenge == null || challenge.Consumed || challenge.ExpiresAt <= DateTime.UtcNow || challenge.Attempts >= 5) return null;
        challenge.Attempts++;
        var digest = PhoneCodeDigest(challenge.Salt, code);
        if (!CryptographicOperations.FixedTimeEquals(digest, Convert.FromHexString(challenge.Digest)))
        { await _context.SaveChangesAsync(); await tx.CommitAsync(); return null; }
        challenge.Consumed = true;
        await _context.SaveChangesAsync();
        var owners = await _context.Users.CountAsync(u => u.Phone == phone && u.IsPhoneNumberVerified && !u.IsDeleted && (u.SuspendedAt == null || u.SuspendedAt <= DateTime.UtcNow) && u.PartnerID == null);
        if (owners != 1) { await tx.CommitAsync(); return null; }
        var user = await GetUserWithClaimsData(u => u.Phone == phone && u.IsPhoneNumberVerified && !u.IsDeleted && (u.SuspendedAt == null || u.SuspendedAt <= DateTime.UtcNow) && u.PartnerID == null);
        if (user == null) { await tx.CommitAsync(); return null; }
        var session = await IssueSession(user, BuildUserClaims(user), ip, userAgent);
        await tx.CommitAsync();
        return session;
    }
}
