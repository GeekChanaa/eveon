namespace VoltaXApi.Dtos;

public class LoginResultDto
{
    /// <summary>Short lived JWT sent as the Authorization header.</summary>
    public string Token { get; set; }

    /// <summary>
    /// Long lived opaque token used to obtain a new access token. Only the client that
    /// received it holds the raw value; the API stores a hash.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>UTC instant <see cref="Token"/> stops being accepted.</summary>
    public DateTime? AccessTokenExpiresAt { get; set; }

    /// <summary>UTC instant <see cref="RefreshToken"/> stops being accepted.</summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public int UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }

    /// <summary>
    /// True when the password was right but the account uses 2FA: no tokens are issued,
    /// the client posts <see cref="TwoFactorToken"/> and the code to /api/auth/verify-2fa.
    /// </summary>
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }

    /// <summary>Auth:RequireTwoFactorForAdmins is on and this admin must enroll before using the dashboard.</summary>
    public bool TwoFactorEnrollmentRequired { get; set; }
}
