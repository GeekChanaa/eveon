using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

namespace VoltaXApi.Ocpi.Services
{
    // Follows OCPP TransactionEvents of roaming tokens and keeps OcpiSessions up to date; queues the
    // session updates and, at the end, the CDR for the owning eMSP.
    public class OcpiTransactionObserver : IRoamingTransactionObserver
    {
        private readonly VoltaXApiDbContext _db;
        private readonly OcpiPartyService _parties;
        private readonly OcpiDataService _data;
        private readonly IExternalTokenAuthorizer _authorizer;
        private readonly IMemoryCache _cache;
        private readonly GlobalConfigurations _globalConfigurations;
        private readonly OcpiOptions _options;
        private readonly ILogger<OcpiTransactionObserver> _logger;

        public OcpiTransactionObserver(VoltaXApiDbContext db, OcpiPartyService parties, OcpiDataService data,
            IExternalTokenAuthorizer authorizer, IMemoryCache cache, GlobalConfigurations globalConfigurations,
            IOptions<OcpiOptions> options, ILogger<OcpiTransactionObserver> logger)
        {
            _db = db;
            _parties = parties;
            _data = data;
            _authorizer = authorizer;
            _cache = cache;
            _globalConfigurations = globalConfigurations;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AuthorizationStatusEnumType?> OnTransactionEventAsync(string chargePointId, TransactionEventRequest request, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled) return null;
            var transactionId = request.TransactionInfo?.TransactionId;
            if (string.IsNullOrEmpty(transactionId)) return null;
            try
            {
                var chargePoint = await _db.ChargePoints.AsNoTracking().FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId, cancellationToken);
                if (chargePoint == null) return null;

                var session = await _db.OcpiSessions.FirstOrDefaultAsync(s => s.ChargePointID == chargePoint.ID && s.TransactionUid == transactionId, cancellationToken);
                AuthorizationStatusEnumType status = AuthorizationStatusEnumType.Accepted;
                if (session == null)
                {
                    var created = await LinkOrCreateAsync(chargePoint, request, transactionId, cancellationToken);
                    if (created == null) return null;
                    (session, status) = created.Value;
                    if (session == null) return status;
                }

                var timestamp = ParseTimestamp(request.Timestamp);
                var meter = EnergyKwh(request.MeterValue);
                if (meter.HasValue)
                {
                    session.MeterStartKwh ??= meter.Value;
                    session.Kwh = Math.Max(session.Kwh, meter.Value - session.MeterStartKwh.Value);
                }
                if (session.Status is OcpiSessionStatus.Pending) session.Status = OcpiSessionStatus.Active;
                if (request.EventType == TransactionEventEnumType.Ended)
                {
                    session.Status = OcpiSessionStatus.Completed;
                    session.EndDateTime = timestamp;
                }
                session.LastUpdated = DateTime.UtcNow;
                await QueueSessionAsync(session, cancellationToken);
                if (session.Status == OcpiSessionStatus.Completed && session.CdrQueuedAt == null)
                    await QueueCdrAsync(session, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return status;
            }
            catch (Exception ex)
            {
                // Never break the charger message flow because of roaming bookkeeping.
                _logger.LogError(ex, "OCPI session tracking failed for transaction {TransactionId} on {ChargePointId}", transactionId, chargePointId);
                return null;
            }
        }

        // Null: not a roaming transaction. (null session, status): roaming token refused.
        private async Task<(OcpiSession? Session, AuthorizationStatusEnumType Status)?> LinkOrCreateAsync(
            ChargePoint chargePoint, TransactionEventRequest request, string transactionId, CancellationToken cancellationToken)
        {
            var connector = await FindConnectorAsync(chargePoint.ID, request.EVSE, cancellationToken);
            var remoteStartId = request.TransactionInfo?.RemoteStartId;
            if (remoteStartId.HasValue)
            {
                var commanded = await _db.OcpiSessions.FirstOrDefaultAsync(s => s.ID == remoteStartId.Value && s.ChargePointID == chargePoint.ID
                    && s.Status == OcpiSessionStatus.Pending && s.TransactionUid == null, cancellationToken);
                if (commanded != null)
                {
                    commanded.TransactionUid = transactionId;
                    commanded.StartDateTime = ParseTimestamp(request.Timestamp);
                    if (request.EVSE != null) commanded.EvseId = request.EVSE.Id;
                    if (connector != null) ApplyTariff(commanded, connector);
                    return (commanded, AuthorizationStatusEnumType.Accepted);
                }
            }

            var idToken = request.IdToken?.IdToken;
            if (string.IsNullOrEmpty(idToken)) return null;
            if (await _db.Cards.AnyAsync(c => c.CardNumber == idToken, cancellationToken)) return null;
            var status = await _authorizer.AuthorizeAsync(idToken, chargePoint.ChargePointId, cancellationToken);
            if (status == null) return null;
            if (status != AuthorizationStatusEnumType.Accepted) return (null, status.Value);

            var authorization = _cache.Get<OcpiAuthorization>(OcpiTokenAuthorizer.CacheKey(chargePoint.ChargePointId, idToken));
            var token = authorization != null
                ? await _db.OcpiTokens.AsNoTracking().FirstOrDefaultAsync(t => t.ID == authorization.TokenId, cancellationToken)
                : await _db.OcpiTokens.AsNoTracking().Where(t => t.Uid == idToken).OrderByDescending(t => t.LastUpdated).FirstOrDefaultAsync(cancellationToken);
            if (token == null) return null;

            var session = new OcpiSession
            {
                OcpiPartyID = token.OcpiPartyID,
                TokenCountryCode = token.CountryCode,
                TokenPartyId = token.PartyId,
                TokenUid = token.Uid,
                TokenType = token.Type,
                ContractId = token.ContractId,
                AuthMethod = authorization?.AuthMethod ?? "WHITELIST",
                AuthorizationReference = authorization?.AuthorizationReference,
                ChargingStationID = chargePoint.ChargingStationID,
                ChargePointID = chargePoint.ID,
                EvseId = request.EVSE?.Id ?? connector?.EvseID ?? 1,
                TransactionUid = transactionId,
                Status = OcpiSessionStatus.Active,
                StartDateTime = ParseTimestamp(request.Timestamp),
                VatRate = _globalConfigurations.Vat,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            if (connector != null) ApplyTariff(session, connector);
            _db.OcpiSessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OCPI session {SessionID} started for {Country}-{Party} on {ChargePointId}", session.ID, token.CountryCode, token.PartyId, chargePoint.ChargePointId);
            return (session, AuthorizationStatusEnumType.Accepted);
        }

        public static void ApplyTariff(OcpiSession session, Connector connector)
        {
            session.ConnectorID = connector.ID;
            session.PricePerKWh = connector.PricePerKWh;
            session.PricePerMinute = connector.PricePerMinute;
            session.PricePerIdleMinute = connector.PricePerIdleMinute;
            session.FlatFee = connector.FlatFee;
        }

        private async Task<Connector?> FindConnectorAsync(int chargePointId, EVSEType? evse, CancellationToken cancellationToken)
        {
            if (evse == null) return null;
            var query = _db.Connectors.AsNoTracking().Where(c => c.ChargePointID == chargePointId && c.EvseID == evse.Id);
            if (evse.ConnectorId.HasValue) query = query.Where(c => c.ConnectorID == evse.ConnectorId);
            return await query.OrderBy(c => c.ConnectorID).FirstOrDefaultAsync(cancellationToken);
        }

        private async Task QueueSessionAsync(OcpiSession session, CancellationToken cancellationToken)
        {
            var party = await _db.OcpiParties.AsNoTracking().FirstOrDefaultAsync(p => p.ID == session.OcpiPartyID && p.Status == OcpiPartyStatus.Registered, cancellationToken);
            var url = party == null ? null : OcpiPartyService.EndpointUrl(party, "sessions", "RECEIVER");
            if (url == null) return;
            var dto = (await _data.ToSessionsAsync(new List<OcpiSession> { session }, cancellationToken)).Single();
            await _parties.EnqueueAsync(party!.ID, "sessions", HttpMethod.Put,
                $"{url}/{_options.CountryCode}/{_options.PartyId}/{dto.Id}", dto, coalesce: true, cancellationToken: cancellationToken);
        }

        private async Task QueueCdrAsync(OcpiSession session, CancellationToken cancellationToken)
        {
            var party = await _db.OcpiParties.AsNoTracking().FirstOrDefaultAsync(p => p.ID == session.OcpiPartyID && p.Status == OcpiPartyStatus.Registered, cancellationToken);
            var url = party == null ? null : OcpiPartyService.EndpointUrl(party, "cdrs", "RECEIVER");
            var cdr = (await _data.ToCdrsAsync(new List<OcpiSession> { session }, cancellationToken)).SingleOrDefault();
            if (cdr == null) return;
            session.CdrQueuedAt = DateTime.UtcNow;
            // Without a CDR receiver the eMSP pulls the CDR through GET /cdrs.
            if (url != null)
                await _parties.EnqueueAsync(party!.ID, "cdrs", HttpMethod.Post, url, cdr, coalesce: false, cancellationToken: cancellationToken);
        }

        private static DateTime ParseTimestamp(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed : DateTime.UtcNow;

        // Energy.Active.Import.Register in kWh (the transaction meter, not a delta).
        public static double? EnergyKwh(IEnumerable<MeterValueType>? meterValues)
        {
            double? last = null;
            foreach (var sample in (meterValues ?? Enumerable.Empty<MeterValueType>()).OrderBy(m => m.Timestamp)
                         .SelectMany(m => m.SampledValue ?? new List<SampledValueType>()))
            {
                if (sample.Measurand is not (null or MeasurandEnumType.Energy_Active_Import_Register) || sample.Phase != null) continue;
                var value = sample.Value * Math.Pow(10, sample.UnitOfMeasure?.Multiplier ?? 0);
                var unit = sample.UnitOfMeasure?.Unit;
                last = unit is "kWh" ? value : value / 1000.0;
            }
            return last;
        }
    }
}
