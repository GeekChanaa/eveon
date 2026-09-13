using System.Security.Claims;
using Google.Apis.Auth;
using VoltaXApi.Dtos;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Services
{
    public class GoogleAuthProvider : IGoogleAuthProvider
    {
        public const string PictureClaimType = "urn:google:picture";
        public const string EmailVerifiedClaimType = "urn:google:email_verified";

        private readonly IConfiguration _config;
        private readonly ILogger<GoogleAuthProvider> _logger;

        public GoogleAuthProvider(IConfiguration config, ILogger<GoogleAuthProvider> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ExternalUserInfoDto> ValidateIdTokenAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ValidationException("Google ID token is required");

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = GetAllowedAudiences()
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Rejected an invalid Google ID token");
                throw new UnauthorizedException("Invalid Google token");
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
                throw new UnauthorizedException("The Google account did not expose an email address");

            return new ExternalUserInfoDto
            {
                ProviderKey = payload.Subject,
                Email = payload.Email,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                PictureUrl = payload.Picture,
                EmailVerified = payload.EmailVerified
            };
        }

        public ExternalUserInfoDto FromPrincipal(ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new UnauthorizedException("Google authentication did not return a principal");

            var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedException("The Google account did not expose an email address");

            var emailVerifiedClaim = principal.FindFirstValue(EmailVerifiedClaimType);

            return new ExternalUserInfoDto
            {
                ProviderKey = providerKey,
                Email = email,
                FirstName = principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = principal.FindFirstValue(ClaimTypes.Surname),
                PictureUrl = principal.FindFirstValue(PictureClaimType),
                // Google only hands the flag over on the userinfo endpoint, assume verified when absent
                EmailVerified = emailVerifiedClaim == null
                    || string.Equals(emailVerifiedClaim, "true", StringComparison.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// The web client id plus any extra client ids (android / ios / desktop) declared in configuration.
        /// </summary>
        private List<string> GetAllowedAudiences()
        {
            var audiences = new List<string>();

            var clientId = _config["Authentication:Google:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId))
                audiences.Add(clientId);

            var additional = _config.GetSection("Authentication:Google:AdditionalClientIds").Get<string[]>();
            if (additional != null)
                audiences.AddRange(additional.Where(a => !string.IsNullOrWhiteSpace(a)));

            if (audiences.Count == 0)
                throw new ValidationException("Google authentication is not configured on this server");

            return audiences;
        }
    }
}
