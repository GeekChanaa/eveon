using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Issues, rotates and revokes refresh tokens. Kept apart from
    /// <see cref="IAuthService"/> so the customer and the partner portals share exactly
    /// the same session handling.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Mints a refresh token for <paramref name="user"/> and returns the raw value —
        /// the only moment it exists outside the client.
        /// </summary>
        Task<(string Token, DateTime ExpiresAt)> Issue(int userID, string? ipAddress, string? userAgent);

        /// <summary>
        /// Spends <paramref name="rawToken"/> and returns a brand new access / refresh pair.
        /// Throws <see cref="Exceptions.UnauthorizedException"/> when the token is unknown,
        /// expired, already spent or belongs to a suspended account.
        /// </summary>
        Task<LoginResultDto> Rotate(string rawToken, string? ipAddress, string? userAgent);

        /// <summary>Revokes a single token — what a sign out does. Unknown tokens are ignored.</summary>
        Task Revoke(string rawToken, string reason = "logout");

        /// <summary>Revokes every active token of an account (password change, suspension, "sign out everywhere").</summary>
        Task RevokeAllForUser(int userID, string reason = "revoke-all");
    }
}
