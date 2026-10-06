using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos;

public class RefreshTokenRequestDto
{
    /// <summary>
    /// The raw refresh token handed out by the previous login or refresh. Optional: browsers
    /// send it in the HttpOnly refresh cookie instead, mobile clients in the body.
    /// </summary>
    public string? RefreshToken { get; set; }
}
