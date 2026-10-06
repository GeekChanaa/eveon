using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

namespace VoltaXApi.Ocpi.Services
{
    public sealed record OcpiAuthorization(int PartyId, int TokenId, string AuthMethod, string? AuthorizationReference);

    // Authorize hook for roaming tokens, applying the OCPI whitelist rules:
    //  ALWAYS: accepted locally; ALLOWED / ALLOWED_OFFLINE: real-time check, accepted if the eMSP
    //  cannot be reached; NEVER: real-time check only.
    public class OcpiTokenAuthorizer : IExternalTokenAuthorizer
    {
        private static readonly TimeSpan RealTimeTimeout = TimeSpan.FromSeconds(5);
        private readonly VoltaXApiDbContext _db;
        private readonly OcpiPartyService _parties;
        private readonly OcpiClient _client;
        private readonly IMemoryCache _cache;
        private readonly OcpiOptions _options;
        private readonly ILogger<OcpiTokenAuthorizer> _logger;

        public OcpiTokenAuthorizer(VoltaXApiDbContext db, OcpiPartyService parties, OcpiClient client, IMemoryCache cache,
            IOptions<OcpiOptions> options, ILogger<OcpiTokenAuthorizer> logger)
        {
            _db = db;
            _parties = parties;
            _client = client;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public static string CacheKey(string chargePointId, string idToken) => $"ocpi-auth:{chargePointId}:{idToken}";

        public async Task<AuthorizationStatusEnumType?> AuthorizeAsync(string? idToken, string? chargePointId, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(idToken)) return null;
            try
            {
                var token = await _db.OcpiTokens.AsNoTracking().Include(t => t.OcpiParty)
                    .Where(t => t.Uid == idToken && t.OcpiParty!.Status == OcpiPartyStatus.Registered)
                    .OrderByDescending(t => t.LastUpdated).FirstOrDefaultAsync(cancellationToken);
                if (token == null) return null;

                var chargePoint = chargePointId == null ? null : await _db.ChargePoints.AsNoTracking()
                    .Where(cp => cp.ChargePointId == chargePointId).Select(cp => new { cp.ID, cp.ChargingStationID }).FirstOrDefaultAsync(cancellationToken);

                // A START_SESSION command already carries the eMSP authorization.
                var since = DateTime.UtcNow.AddMinutes(-10);
                if (chargePoint != null && await _db.OcpiSessions.AnyAsync(s => s.TokenUid == idToken && s.ChargePointID == chargePoint.ID
                        && s.Status == OcpiSessionStatus.Pending && s.AuthMethod == "COMMAND" && s.CreatedAt >= since, cancellationToken))
                    return AuthorizationStatusEnumType.Accepted;

                if (!token.Valid) return AuthorizationStatusEnumType.Blocked;

                AuthorizationStatusEnumType status;
                string authMethod = "WHITELIST";
                string? reference = null;
                if (token.Whitelist == "ALWAYS")
                    status = AuthorizationStatusEnumType.Accepted;
                else
                {
                    var realTime = await RealTimeAsync(token, chargePoint?.ChargingStationID, cancellationToken);
                    if (realTime != null)
                    {
                        status = realTime.Value.Status;
                        reference = realTime.Value.Reference;
                        authMethod = "AUTH_REQUEST";
                    }
                    else
                        status = token.Whitelist == "NEVER" ? AuthorizationStatusEnumType.Invalid : AuthorizationStatusEnumType.Accepted;
                }

                if (status == AuthorizationStatusEnumType.Accepted && chargePointId != null)
                    _cache.Set(CacheKey(chargePointId, idToken), new OcpiAuthorization(token.OcpiPartyID, token.ID, authMethod, reference), TimeSpan.FromMinutes(15));
                _logger.LogInformation("OCPI authorize {Country}-{Party} token ({Whitelist}) => {Status}", token.CountryCode, token.PartyId, token.Whitelist, status);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCPI authorize failed for charge point {ChargePointId}", chargePointId);
                return null;
            }
        }

        private async Task<(AuthorizationStatusEnumType Status, string? Reference)?> RealTimeAsync(OcpiToken token, int? stationId, CancellationToken cancellationToken)
        {
            var party = token.OcpiParty!;
            var url = OcpiPartyService.EndpointUrl(party, "tokens", "SENDER");
            var credential = _parties.OutgoingToken(party);
            if (url == null || credential == null) return null;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RealTimeTimeout);
            try
            {
                var body = stationId.HasValue ? OcpiJson.Serialize(new LocationReferencesDto { LocationId = OcpiMapper.LocationId(stationId.Value) }) : null;
                var info = await _client.SendAsync<AuthorizationInfoDto>(HttpMethod.Post,
                    $"{url}/{Uri.EscapeDataString(token.Uid)}/authorize?type={token.Type}", credential, body,
                    party.CountryCode, party.PartyId, timeout.Token);
                if (info?.Allowed == null) return null;
                return (info.Allowed switch
                {
                    "ALLOWED" => AuthorizationStatusEnumType.Accepted,
                    "BLOCKED" => AuthorizationStatusEnumType.Blocked,
                    "EXPIRED" => AuthorizationStatusEnumType.Expired,
                    "NO_CREDIT" => AuthorizationStatusEnumType.NoCredit,
                    "NOT_ALLOWED" => AuthorizationStatusEnumType.NotAtThisLocation,
                    _ => AuthorizationStatusEnumType.Invalid
                }, info.AuthorizationReference);
            }
            catch (Exception ex) when (ex is OcpiClientException { Permanent: false } or HttpRequestException or OperationCanceledException)
            {
                _logger.LogWarning("OCPI real-time authorization unavailable for party {PartyID}: {Error}", party.ID, ex.Message);
                return null;
            }
            catch (OcpiClientException ex)
            {
                // 2004 (unknown token) and other client errors are a definite answer.
                _logger.LogWarning("OCPI real-time authorization refused for party {PartyID}: {Error}", party.ID, ex.Message);
                return (AuthorizationStatusEnumType.Invalid, null);
            }
        }
    }
}
