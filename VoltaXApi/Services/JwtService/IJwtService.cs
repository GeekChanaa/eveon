
using System.Security.Claims;

namespace VoltaXApi.Services;

/// <summary>One issued access token and the instant it stops being accepted.</summary>
public record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtService
{
    string GenerateToken(List<Claim> claims);

    /// <summary>
    /// Same token as <see cref="GenerateToken"/>, with the expiry the client needs to
    /// know when to refresh instead of waiting for a 401.
    /// </summary>
    AccessToken GenerateAccessToken(List<Claim> claims);

    TimeSpan AccessTokenLifetime { get; }
}
