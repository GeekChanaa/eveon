using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using VoltaXApi.Configurations;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Services;

namespace VoltaXApi.Ocpi.Controllers
{
    [Route("ocpi")]
    [OcpiAuthorize(handshake: true)]
    [EnableRateLimiting(SecurityConfiguration.PublicRateLimit)]
    public class OcpiVersionsController : OcpiControllerBase
    {
        private readonly OcpiOptions _options;

        public OcpiVersionsController(IOptions<OcpiOptions> options)
        {
            _options = options.Value;
        }

        [HttpGet("versions")]
        public IActionResult Versions() =>
            Ocpi(new List<VersionDto> { new() { Version = OcpiOptions.VersionNumber, Url = _options.VersionDetailsUrl } });

        [HttpGet(OcpiOptions.VersionNumber)]
        public IActionResult Details() => Ocpi(BuildDetails(_options));

        public static VersionDetailsDto BuildDetails(OcpiOptions options) => new()
        {
            Version = OcpiOptions.VersionNumber,
            Endpoints = new List<EndpointDto>
            {
                new() { Identifier = "credentials", Role = "SENDER", Url = options.ModuleUrl("credentials") },
                new() { Identifier = "credentials", Role = "RECEIVER", Url = options.ModuleUrl("credentials") },
                new() { Identifier = "locations", Role = "SENDER", Url = options.ModuleUrl("locations") },
                new() { Identifier = "sessions", Role = "SENDER", Url = options.ModuleUrl("sessions") },
                new() { Identifier = "cdrs", Role = "SENDER", Url = options.ModuleUrl("cdrs") },
                new() { Identifier = "tariffs", Role = "SENDER", Url = options.ModuleUrl("tariffs") },
                new() { Identifier = "tokens", Role = "RECEIVER", Url = options.ModuleUrl("tokens") },
                new() { Identifier = "commands", Role = "RECEIVER", Url = options.ModuleUrl("commands") }
            }
        };
    }

    [Route("ocpi/" + OcpiOptions.VersionNumber + "/credentials")]
    [OcpiAuthorize(handshake: true)]
    [EnableRateLimiting(SecurityConfiguration.PublicRateLimit)]
    public class OcpiCredentialsController : OcpiControllerBase
    {
        private readonly OcpiCredentialsService _credentials;

        public OcpiCredentialsController(OcpiCredentialsService credentials)
        {
            _credentials = credentials;
        }

        [HttpGet]
        public IActionResult Get() => Ocpi(_credentials.OurCredentials(CallerToken));

        [HttpPost]
        public Task<IActionResult> Post([FromBody] CredentialsDto? body, CancellationToken cancellationToken)
        {
            // POST is only for the first registration (token A); a registered party uses PUT.
            if (!CalledWithTokenA)
                return Task.FromResult(OcpiError(StatusCodes.Status405MethodNotAllowed, OcpiStatusCodes.ClientError, "Already registered, use PUT."));
            return Register(body, cancellationToken);
        }

        [HttpPut]
        public Task<IActionResult> Put([FromBody] CredentialsDto? body, CancellationToken cancellationToken)
        {
            if (CalledWithTokenA || Party.Status != Models.OcpiPartyStatus.Registered)
                return Task.FromResult(OcpiError(StatusCodes.Status405MethodNotAllowed, OcpiStatusCodes.ClientError, "Not registered, use POST."));
            return Register(body, cancellationToken);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(CancellationToken cancellationToken)
        {
            if (CalledWithTokenA || Party.Status != Models.OcpiPartyStatus.Registered)
                return OcpiError(StatusCodes.Status405MethodNotAllowed, OcpiStatusCodes.ClientError, "Not registered.");
            await _credentials.UnregisterAsync(Party, notifyPartner: false, cancellationToken);
            return Ocpi<object?>(null);
        }

        private async Task<IActionResult> Register(CredentialsDto? body, CancellationToken cancellationToken)
        {
            if (body == null) return OcpiError(StatusCodes.Status400BadRequest, OcpiStatusCodes.InvalidParameters, "Credentials object expected.");
            try
            {
                return Ocpi(await _credentials.AcceptPartnerCredentialsAsync(Party, body, cancellationToken));
            }
            catch (OcpiRegistrationException ex)
            {
                return OcpiError(ex.StatusCode >= 3000 ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest, ex.StatusCode, ex.Message);
            }
        }
    }
}
