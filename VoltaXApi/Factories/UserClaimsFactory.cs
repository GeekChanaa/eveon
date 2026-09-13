using System.Security.Claims;
using VoltaXApi.Models;

namespace VoltaXApi.Factories
{
    public class UserClaimsFactory : IUserClaimsFactory
    {
        public List<Claim> BuildUserClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("emailVerified", user.IsEmailVerified.ToString()),
                new Claim("phoneVerified", user.IsPhoneNumberVerified.ToString()),
            };

            AddPermissionClaims(claims, user);

            return claims;
        }

        public List<Claim> BuildPartnerClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("partnerID", user.PartnerID?.ToString() ?? ""),
                new Claim("emailVerified", user.IsEmailVerified.ToString()),
                new Claim("phoneVerified", user.IsPhoneNumberVerified.ToString()),
            };

            AddPermissionClaims(claims, user);

            return claims;
        }

        public List<Claim> BuildClaimsFor(User user)
            => user.PartnerID != null ? BuildPartnerClaims(user) : BuildUserClaims(user);

        private static void AddPermissionClaims(List<Claim> claims, User user)
        {
            if (user.Role?.RolePermissions == null)
                return;

            foreach (var rolePermission in user.Role.RolePermissions)
            {
                if (rolePermission.Permission == null)
                    continue;

                claims.Add(new Claim("permission", rolePermission.Permission.Name));
                claims.Add(new Claim($"permission_scope:{rolePermission.Permission.Name}", rolePermission.Scope.ToString()));
            }
        }
    }
}
