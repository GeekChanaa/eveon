using System.Security.Claims;
using VoltaXApi.Models;

namespace VoltaXApi.Factories
{
    /// <summary>
    /// Single place the JWT claim set is built, so a token minted by a login, by a
    /// verification step or by a refresh always carries the same claims.
    /// </summary>
    public interface IUserClaimsFactory
    {
        /// <summary>Claims for a customer / administrator account.</summary>
        List<Claim> BuildUserClaims(User user);

        /// <summary>Claims for a partner account — adds partnerID, drops the verification flags.</summary>
        List<Claim> BuildPartnerClaims(User user);

        /// <summary>Picks the right set from the account itself. Used by the refresh flow.</summary>
        List<Claim> BuildClaimsFor(User user);
    }
}
