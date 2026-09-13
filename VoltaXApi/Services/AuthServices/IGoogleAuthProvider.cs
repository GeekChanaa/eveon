using System.Security.Claims;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IGoogleAuthProvider
    {
        /// <summary>
        /// Validates a Google issued ID token (signature, issuer, audience, expiry) and
        /// projects it onto the provider agnostic <see cref="ExternalUserInfoDto"/>.
        /// </summary>
        Task<ExternalUserInfoDto> ValidateIdTokenAsync(string idToken);

        /// <summary>
        /// Projects the principal produced by the ASP.NET Core Google handler (redirect flow)
        /// onto the provider agnostic <see cref="ExternalUserInfoDto"/>.
        /// </summary>
        ExternalUserInfoDto FromPrincipal(ClaimsPrincipal principal);
    }
}
