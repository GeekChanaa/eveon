using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos;

public class RefreshTokenRequestDto
{
    /// <summary>The raw refresh token handed out by the previous login or refresh.</summary>
    [Required]
    public string RefreshToken { get; set; }
}
