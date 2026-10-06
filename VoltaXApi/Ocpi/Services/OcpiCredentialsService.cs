using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;

namespace VoltaXApi.Ocpi.Services
{
    public class OcpiRegistrationException : Exception
    {
        public OcpiRegistrationException(int statusCode, string message) : base(message) { StatusCode = statusCode; }
        public int StatusCode { get; }
    }

    // Credentials module and the registration handshake, in both directions.
    public class OcpiCredentialsService
    {
        private readonly VoltaXApiDbContext _db;
        private readonly OcpiPartyService _parties;
        private readonly OcpiClient _client;
        private readonly OcpiOptions _options;
        private readonly ILogger<OcpiCredentialsService> _logger;

        public OcpiCredentialsService(VoltaXApiDbContext db, OcpiPartyService parties, OcpiClient client,
            IOptions<OcpiOptions> options, ILogger<OcpiCredentialsService> logger)
        {
            _db = db;
            _parties = parties;
            _client = client;
            _options = options.Value;
            _logger = logger;
        }

        public CredentialsDto OurCredentials(string token) => new()
        {
            Token = token,
            Url = _options.VersionsUrl,
            Roles = new List<CredentialsRoleDto>
            {
                new()
                {
                    Role = "CPO",
                    CountryCode = _options.CountryCode,
                    PartyId = _options.PartyId,
                    BusinessDetails = new BusinessDetailsDto { Name = _options.BusinessName, Website = _options.Website }
                }
            }
        };

        // Dashboard: token A handed to a partner out of band; they register with it.
        public async Task<(OcpiParty Party, string TokenA)> CreateTokenAAsync(string name)
        {
            var token = OcpiTokens.Generate();
            var party = new OcpiParty
            {
                Name = name.Trim(),
                Status = OcpiPartyStatus.Pending,
                TokenAHash = OcpiTokens.Hash(token),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.OcpiParties.Add(party);
            await _db.SaveChangesAsync();
            _logger.LogInformation("OCPI token A created for partner {PartyID} ({Name})", party.ID, party.Name);
            return (party, token);
        }

        // Partner -> us: POST (first registration, token A) or PUT (update, current token).
        public async Task<CredentialsDto> AcceptPartnerCredentialsAsync(OcpiParty party, CredentialsDto theirs, CancellationToken cancellationToken)
        {
            ValidateCredentials(theirs);
            var (version, endpoints) = await FetchEndpointsAsync(theirs.Url, theirs.Token, cancellationToken);
            var role = PrimaryRole(theirs.Roles);
            if (await _db.OcpiParties.AnyAsync(p => p.ID != party.ID && p.Status == OcpiPartyStatus.Registered
                    && p.CountryCode == role.CountryCode && p.PartyId == role.PartyId, cancellationToken))
                throw new OcpiRegistrationException(OcpiStatusCodes.ClientError, "This party is already registered with other credentials.");

            var tokenC = OcpiTokens.Generate();
            ApplyRegistration(party, theirs, role, version, endpoints);
            party.IncomingTokenHash = OcpiTokens.Hash(tokenC);
            party.TokenAHash = null;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OCPI partner {PartyID} ({Country}-{Party}) registered", party.ID, party.CountryCode, party.PartyId);
            return OurCredentials(tokenC);
        }

        // Us -> partner: dashboard gives their versions URL and the token A they issued.
        public async Task<OcpiParty> RegisterWithPartnerAsync(string name, string versionsUrl, string tokenA, CancellationToken cancellationToken)
        {
            var (version, endpoints) = await FetchEndpointsAsync(versionsUrl, tokenA, cancellationToken);
            var credentialsUrl = endpoints.FirstOrDefault(e => e.Identifier == "credentials")?.Url
                ?? throw new OcpiRegistrationException(OcpiStatusCodes.NoMatchingEndpoints, "The partner has no credentials endpoint.");

            // Token B must be valid before the POST: the partner calls our versions endpoint with it.
            var tokenB = OcpiTokens.Generate();
            var party = new OcpiParty
            {
                Name = name.Trim(),
                Status = OcpiPartyStatus.Pending,
                VersionsUrl = versionsUrl,
                Version = version,
                EndpointsJson = OcpiJson.Serialize(endpoints),
                IncomingTokenHash = OcpiTokens.Hash(tokenB),
                OutgoingTokenProtected = _parties.Protect(tokenA),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.OcpiParties.Add(party);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var theirs = await _client.SendAsync<CredentialsDto>(HttpMethod.Post, credentialsUrl, tokenA,
                    OcpiJson.Serialize(OurCredentials(tokenB)), cancellationToken: cancellationToken)
                    ?? throw new OcpiRegistrationException(OcpiStatusCodes.UnableToUseClientApi, "The partner returned no credentials.");
                ValidateCredentials(theirs);
                var role = PrimaryRole(theirs.Roles);
                // Endpoints may differ once authenticated with token C.
                try
                {
                    (version, endpoints) = await FetchEndpointsAsync(theirs.Url, theirs.Token, cancellationToken);
                }
                catch (Exception ex) when (ex is OcpiClientException or OcpiRegistrationException or HttpRequestException)
                {
                    _logger.LogWarning("OCPI partner {PartyID}: endpoints could not be refreshed with token C: {Error}", party.ID, ex.Message);
                }
                ApplyRegistration(party, theirs, role, version, endpoints);
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("OCPI registration with {Country}-{Party} completed", party.CountryCode, party.PartyId);
                return party;
            }
            catch (Exception ex) when (ex is OcpiClientException or OcpiRegistrationException or HttpRequestException or TaskCanceledException)
            {
                party.Status = OcpiPartyStatus.Unregistered;
                party.IncomingTokenHash = null;
                party.OutgoingTokenProtected = null;
                party.LastError = Truncate(ex.Message);
                await _db.SaveChangesAsync(CancellationToken.None);
                throw new OcpiRegistrationException(OcpiStatusCodes.UnableToUseClientApi, "Registration failed: " + ex.Message);
            }
        }

        // Partner asked (DELETE) or admin unregistered: both tokens stop working.
        public async Task UnregisterAsync(OcpiParty party, bool notifyPartner, CancellationToken cancellationToken)
        {
            if (notifyPartner && party.Status == OcpiPartyStatus.Registered)
            {
                var url = OcpiPartyService.EndpointUrl(party, "credentials", "RECEIVER") ?? OcpiPartyService.EndpointUrl(party, "credentials", "SENDER");
                var token = _parties.OutgoingToken(party);
                if (url != null && token != null)
                {
                    try
                    {
                        await _client.SendAsync<object>(HttpMethod.Delete, url, token, null, party.CountryCode, party.PartyId, cancellationToken);
                    }
                    catch (Exception ex) when (ex is OcpiClientException or HttpRequestException or TaskCanceledException)
                    {
                        _logger.LogWarning("OCPI partner {PartyID}: DELETE credentials failed: {Error}", party.ID, ex.Message);
                    }
                }
            }
            party.Status = OcpiPartyStatus.Unregistered;
            party.IncomingTokenHash = null;
            party.OutgoingTokenProtected = null;
            party.TokenAHash = null;
            party.UpdatedAt = DateTime.UtcNow;
            var pending = await _db.OcpiOutbox.Where(m => m.OcpiPartyID == party.ID && m.Status == OcpiOutboxStatus.Pending).ToListAsync(cancellationToken);
            foreach (var message in pending)
            {
                message.Status = OcpiOutboxStatus.Failed;
                message.LastError = "Partner unregistered";
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RefreshEndpointsAsync(OcpiParty party, CancellationToken cancellationToken)
        {
            var token = _parties.OutgoingToken(party) ?? throw new OcpiRegistrationException(OcpiStatusCodes.ClientError, "No token for this partner.");
            if (string.IsNullOrEmpty(party.VersionsUrl)) throw new OcpiRegistrationException(OcpiStatusCodes.ClientError, "No versions URL for this partner.");
            var (version, endpoints) = await FetchEndpointsAsync(party.VersionsUrl, token, cancellationToken);
            party.Version = version;
            party.EndpointsJson = OcpiJson.Serialize(endpoints);
            party.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<(string Version, List<EndpointDto> Endpoints)> FetchEndpointsAsync(string versionsUrl, string token, CancellationToken cancellationToken)
        {
            List<VersionDto>? versions;
            try
            {
                versions = await _client.SendAsync<List<VersionDto>>(HttpMethod.Get, versionsUrl, token, null, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is OcpiClientException or HttpRequestException or TaskCanceledException or JsonException)
            {
                throw new OcpiRegistrationException(OcpiStatusCodes.UnableToUseClientApi, "Could not read the partner versions: " + ex.Message);
            }
            var match = versions?.FirstOrDefault(v => v.Version == OcpiOptions.VersionNumber)
                ?? throw new OcpiRegistrationException(OcpiStatusCodes.UnsupportedVersion, "The partner does not support OCPI 2.2.1.");
            VersionDetailsDto? details;
            try
            {
                details = await _client.SendAsync<VersionDetailsDto>(HttpMethod.Get, match.Url, token, null, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is OcpiClientException or HttpRequestException or TaskCanceledException or JsonException)
            {
                throw new OcpiRegistrationException(OcpiStatusCodes.UnableToUseClientApi, "Could not read the partner endpoints: " + ex.Message);
            }
            if (details?.Endpoints == null || details.Endpoints.Count == 0)
                throw new OcpiRegistrationException(OcpiStatusCodes.NoMatchingEndpoints, "The partner declared no endpoints.");
            return (match.Version, details.Endpoints);
        }

        private static void ValidateCredentials(CredentialsDto? credentials)
        {
            if (credentials == null || string.IsNullOrWhiteSpace(credentials.Token) || credentials.Token.Length > 64
                || !Uri.TryCreate(credentials.Url, UriKind.Absolute, out _) || credentials.Roles == null || credentials.Roles.Count == 0
                || credentials.Roles.Any(r => r.CountryCode?.Length != 2 || r.PartyId?.Length != 3 || string.IsNullOrWhiteSpace(r.Role)))
                throw new OcpiRegistrationException(OcpiStatusCodes.InvalidParameters, "Invalid credentials object.");
        }

        // The role we exchange data with: the eMSP, else a hub, else the first declared.
        private static CredentialsRoleDto PrimaryRole(List<CredentialsRoleDto> roles) =>
            roles.FirstOrDefault(r => r.Role == "EMSP") ?? roles.FirstOrDefault(r => r.Role == "HUB") ?? roles[0];

        private void ApplyRegistration(OcpiParty party, CredentialsDto theirs, CredentialsRoleDto role, string version, List<EndpointDto> endpoints)
        {
            party.CountryCode = role.CountryCode.ToUpperInvariant();
            party.PartyId = role.PartyId.ToUpperInvariant();
            party.Role = role.Role;
            party.RolesJson = OcpiJson.Serialize(theirs.Roles);
            if (string.IsNullOrWhiteSpace(party.Name)) party.Name = role.BusinessDetails?.Name ?? party.PartyId;
            party.VersionsUrl = theirs.Url;
            party.Version = version;
            party.EndpointsJson = OcpiJson.Serialize(endpoints);
            party.OutgoingTokenProtected = _parties.Protect(theirs.Token);
            party.Status = OcpiPartyStatus.Registered;
            party.RegisteredAt = DateTime.UtcNow;
            party.LastError = null;
            party.UpdatedAt = DateTime.UtcNow;
        }

        private static string Truncate(string value) => value.Length <= 1024 ? value : value[..1024];
    }
}
