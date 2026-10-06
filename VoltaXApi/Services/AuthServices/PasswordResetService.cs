using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Services;

public interface IPasswordResetService
{
    /// <summary>Emails a single use reset link. Silent for unknown addresses (no account enumeration).</summary>
    Task RequestLinkReset(string email, bool partnerPortal);

    /// <summary>Emails a 6 digit code to the mobile app. Silent for unknown addresses.</summary>
    Task RequestMobileCode(string email);

    /// <summary>Spends the mobile code (max 5 attempts) and returns a fresh single use reset token.</summary>
    Task<string> VerifyMobileCode(string email, string code);

    Task ResetPassword(string email, string token, string newPassword);

    /// <summary>Creates a reset link for an account that is known to exist.</summary>
    Task<string> CreateResetLink(User user, bool partnerPortal);
}

/// <summary>
/// Reset tokens are 256 bit random values stored as SHA-256; mobile codes are stored as an
/// HMAC keyed with the signing key, since 10^6 possible codes are trivial to hash offline.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    public const int MaxCodeAttempts = 5;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private const string InvalidCode = "Invalid or expired code. Please request a new one.";
    private const string InvalidToken = "This reset link is invalid or has expired. Please request a new one.";

    private readonly VoltaXApiDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMailService _mailService;
    private readonly IMailRequestFactory _mailRequestFactory;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        VoltaXApiDbContext context,
        IConfiguration config,
        IMailService mailService,
        IMailRequestFactory mailRequestFactory,
        IRefreshTokenService refreshTokenService,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _config = config;
        _mailService = mailService;
        _mailRequestFactory = mailRequestFactory;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task RequestLinkReset(string email, bool partnerPortal)
    {
        var user = await FindUser(email);
        if (user == null || (partnerPortal && user.PartnerID == null))
        {
            _logger.LogInformation("Password reset requested for an unknown or ineligible account");
            return;
        }

        var link = await CreateResetLink(user, partnerPortal);
        var request = _mailRequestFactory.CreateResetPasswordMailRequest(user.Email);
        await _mailService.SendResetPasswordMailRequest(request, user.FullName, link);
    }

    public async Task<string> CreateResetLink(User user, bool partnerPortal)
    {
        var raw = IssueToken(user, TokenLifetime);
        await _context.SaveChangesAsync();

        string spaLink = _config["SpaLink"] ?? "";
        if (!spaLink.EndsWith("/")) spaLink += "/";
        var path = partnerPortal ? "partner-auth/reset-password" : "auth/reset-password";
        return $"{spaLink}{path}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(raw)}";
    }

    public async Task RequestMobileCode(string email)
    {
        var user = await FindUser(email);
        if (user == null)
            return;

        var code = AuthHelper.GenerateNumericCode(6);
        user.ResetPasswordCode = CodeDigest(user, code);
        user.ResetPasswordCodeExpiresAt = DateTime.UtcNow.Add(CodeLifetime);
        user.ResetPasswordCodeAttempts = 0;
        await _context.SaveChangesAsync();

        var request = _mailRequestFactory.CreateResetPasswordForMobileMailRequest(user.Email);
        await _mailService.SendResetPasswordForMobileMailRequest(request, user.FullName, code);
    }

    public async Task<string> VerifyMobileCode(string email, string code)
    {
        var user = await FindUser(email);
        if (user == null || string.IsNullOrEmpty(user.ResetPasswordCode) || string.IsNullOrWhiteSpace(code))
            throw new ValidationException(InvalidCode);

        if (user.ResetPasswordCodeExpiresAt == null || user.ResetPasswordCodeExpiresAt <= DateTime.UtcNow
            || user.ResetPasswordCodeAttempts >= MaxCodeAttempts)
        {
            ClearCode(user);
            await _context.SaveChangesAsync();
            throw new ValidationException(InvalidCode);
        }

        user.ResetPasswordCodeAttempts++;
        if (!AuthHelper.FixedTimeEqualsHex(user.ResetPasswordCode, CodeDigest(user, code.Trim())))
        {
            if (user.ResetPasswordCodeAttempts >= MaxCodeAttempts)
                ClearCode(user);
            await _context.SaveChangesAsync();
            throw new ValidationException(InvalidCode);
        }

        ClearCode(user);
        var raw = IssueToken(user, TimeSpan.FromMinutes(15));
        await _context.SaveChangesAsync();
        return raw;
    }

    public async Task ResetPassword(string email, string token, string newPassword)
    {
        var user = await FindUser(email);
        if (user == null || string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(user.ResetPasswordToken)
            || user.ResetPasswordTokenExpiresAt == null || user.ResetPasswordTokenExpiresAt <= DateTime.UtcNow
            || !AuthHelper.FixedTimeEqualsHex(user.ResetPasswordToken, AuthHelper.Sha256Hex(token)))
            throw new ValidationException(InvalidToken);

        AuthHelper.CreatePasswordHash(newPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.ResetPasswordToken = null;
        user.ResetPasswordTokenExpiresAt = null;
        ClearCode(user);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // The old password is gone, so every session opened with it goes too.
        await _refreshTokenService.RevokeAllForUser(user.ID, "password-reset");
    }

    private static string IssueToken(User user, TimeSpan lifetime)
    {
        var raw = AuthHelper.GenerateSecureToken();
        user.ResetPasswordToken = AuthHelper.Sha256Hex(raw);
        user.ResetPasswordTokenExpiresAt = DateTime.UtcNow.Add(lifetime);
        return raw;
    }

    private static void ClearCode(User user)
    {
        user.ResetPasswordCode = null;
        user.ResetPasswordCodeExpiresAt = null;
        user.ResetPasswordCodeAttempts = 0;
    }

    private string CodeDigest(User user, string code) =>
        AuthHelper.KeyedDigest(_config["AppSettings:Token"]!, "reset-code:" + user.ID, code);

    private Task<User?> FindUser(string email)
    {
        var normalized = (email ?? "").Trim().ToLower();
        return _context.Users.FirstOrDefaultAsync(u => u.Email == normalized);
    }
}
