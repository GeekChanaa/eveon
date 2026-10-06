using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos;

public class EmailRequestDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";
}

public class TwoFactorLoginDto
{
    [Required, MaxLength(2048)]
    public string TwoFactorToken { get; set; } = "";

    /// <summary>The 6 digit TOTP code or one of the recovery codes.</summary>
    [Required, MaxLength(32)]
    public string Code { get; set; } = "";
}

public class TwoFactorCodeDto
{
    [Required, MaxLength(32)]
    public string Code { get; set; } = "";
}

public class TwoFactorDisableDto
{
    [MaxLength(256)]
    public string? Password { get; set; }

    [Required, MaxLength(32)]
    public string Code { get; set; } = "";
}
