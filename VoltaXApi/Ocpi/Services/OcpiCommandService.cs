using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Ocpi.Dtos;
using VoltaXApi.Ocpi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Ocpi.Services
{
    // Commands receiver. The request is validated synchronously and answered with a CommandResponse;
    // the CommandResult goes to response_url through the outbox (retried, survives restarts).
    public class OcpiCommandService
    {
        public const int ResultTimeoutSeconds = 30;
        private readonly VoltaXApiDbContext _db;
        private readonly IOcpiCommandExecutor _executor;
        private readonly OcpiPartyService _parties;
        private readonly OcpiTokenStore _tokens;
        private readonly GlobalConfigurations _globalConfigurations;
        private readonly ILogger<OcpiCommandService> _logger;

        public OcpiCommandService(VoltaXApiDbContext db, IOcpiCommandExecutor executor, OcpiPartyService parties, OcpiTokenStore tokens,
            GlobalConfigurations globalConfigurations, ILogger<OcpiCommandService> logger)
        {
            _db = db;
            _executor = executor;
            _parties = parties;
            _tokens = tokens;
            _globalConfigurations = globalConfigurations;
            _logger = logger;
        }

        public static readonly string[] Commands = { "START_SESSION", "STOP_SESSION", "UNLOCK_CONNECTOR", "RESERVE_NOW", "CANCEL_RESERVATION" };

        public async Task<CommandResponseDto> HandleAsync(OcpiParty party, string command, CommandRequestDto request, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(request.ResponseUrl, UriKind.Absolute, out var responseUri) || responseUri.Scheme is not ("https" or "http"))
                throw new OcpiValidationException("A valid response_url is required.");

            OcpiCommandOutcome? outcome;
            switch (command)
            {
                case "START_SESSION": outcome = await StartSessionAsync(party, request, cancellationToken); break;
                case "STOP_SESSION": outcome = await StopSessionAsync(party, request, cancellationToken); break;
                case "UNLOCK_CONNECTOR": outcome = await UnlockConnectorAsync(request, cancellationToken); break;
                case "RESERVE_NOW": outcome = await ReserveNowAsync(party, request, cancellationToken); break;
                case "CANCEL_RESERVATION": outcome = await CancelReservationAsync(party, request, cancellationToken); break;
                default: return Response("NOT_SUPPORTED", "Unknown command.");
            }
            // Null outcome: synchronous refusal already described in the response.
            if (outcome == null) return Response("REJECTED", "Unknown location, EVSE or connector.");
            if (outcome.Result == "UNKNOWN_SESSION") return Response("UNKNOWN_SESSION", outcome.Message);

            await _parties.EnqueueAsync(party.ID, "commands", HttpMethod.Post, responseUri.ToString(),
                new CommandResultDto
                {
                    Result = outcome.Result,
                    Message = outcome.Message == null ? null : new List<DisplayTextDto> { new() { Text = outcome.Message } }
                }, coalesce: false, cancellationToken: cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OCPI {Command} from party {PartyID} => {Result}", command, party.ID, outcome.Result);
            return Response("ACCEPTED", null);
        }

        private static CommandResponseDto Response(string result, string? message) => new()
        {
            Result = result,
            Timeout = ResultTimeoutSeconds,
            Message = message == null ? null : new List<DisplayTextDto> { new() { Text = message } }
        };

        private async Task<(ChargePoint ChargePoint, int EvseId, List<Connector> Connectors)?> ResolveEvseAsync(string? locationId, string? evseUid, CancellationToken cancellationToken)
        {
            if (!OcpiMapper.TryParseId(locationId, out var stationId)) return null;
            var station = await _db.ChargingStations.AsNoTracking()
                .Include(s => s.ChargePoints!).ThenInclude(cp => cp.Connectors)
                .FirstOrDefaultAsync(s => s.ID == stationId, cancellationToken);
            if (station == null || !OcpiMapper.IsPublished(station)) return null;
            var evses = (station.ChargePoints ?? new List<ChargePoint>()).Where(OcpiMapper.IsPublished)
                .SelectMany(cp => (cp.Connectors ?? new List<Connector>()).GroupBy(c => c.EvseID).Select(g => (cp, g.Key, g.ToList())))
                .ToList();
            if (string.IsNullOrEmpty(evseUid))
                return evses.Count == 1 ? evses[0] : null;
            if (!OcpiMapper.TryParseEvseUid(evseUid, out var chargePointId, out var evseId)) return null;
            var match = evses.FirstOrDefault(e => e.cp.ID == chargePointId && e.Key == evseId);
            return match.cp == null ? null : match;
        }

        private async Task<OcpiCommandOutcome?> StartSessionAsync(OcpiParty party, CommandRequestDto request, CancellationToken cancellationToken)
        {
            var dto = request.Token ?? throw new OcpiValidationException("token is required.");
            if (dto.Uid == null || dto.Type == null || dto.CountryCode == null || dto.PartyId == null)
                throw new OcpiValidationException("token is incomplete.");
            var evse = await ResolveEvseAsync(request.LocationId, request.EvseUid, cancellationToken);
            if (evse == null) return null;
            var (chargePoint, evseId, connectors) = evse.Value;
            var connector = request.ConnectorId == null ? connectors.OrderBy(c => c.ConnectorID).First()
                : connectors.FirstOrDefault(c => OcpiMapper.ConnectorId(c) == request.ConnectorId);
            if (connector == null) return null;

            // The token in the command is the eMSP authorization; keep it for the session.
            dto.LastUpdated ??= DateTime.UtcNow;
            var token = await _tokens.UpsertAsync(party, dto.CountryCode, dto.PartyId, dto.Uid, dto.Type, dto, partial: false, cancellationToken);
            if (!token.Valid) return new OcpiCommandOutcome(OcpiCommandResults.Rejected, "Token is not valid.");

            var session = new OcpiSession
            {
                OcpiPartyID = party.ID,
                TokenCountryCode = token.CountryCode,
                TokenPartyId = token.PartyId,
                TokenUid = token.Uid,
                TokenType = token.Type,
                ContractId = token.ContractId,
                AuthMethod = "COMMAND",
                AuthorizationReference = request.AuthorizationReference,
                ChargingStationID = chargePoint.ChargingStationID,
                ChargePointID = chargePoint.ID,
                EvseId = evseId,
                Status = OcpiSessionStatus.Pending,
                StartDateTime = DateTime.UtcNow,
                VatRate = _globalConfigurations.Vat,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            OcpiTransactionObserver.ApplyTariff(session, connector);
            _db.OcpiSessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);

            var outcome = await _executor.StartSessionAsync(chargePoint.ChargePointId, evseId, token.Uid, token.Type, session.ID);
            if (outcome.Result != OcpiCommandResults.Accepted) session.Status = OcpiSessionStatus.Invalid;
            return outcome;
        }

        private async Task<OcpiCommandOutcome?> StopSessionAsync(OcpiParty party, CommandRequestDto request, CancellationToken cancellationToken)
        {
            if (!OcpiMapper.TryParseId(request.SessionId, out var sessionId)) return new OcpiCommandOutcome("UNKNOWN_SESSION", "Unknown session.");
            var session = await _db.OcpiSessions.AsNoTracking().FirstOrDefaultAsync(s => s.ID == sessionId && s.OcpiPartyID == party.ID, cancellationToken);
            if (session == null || session.Status != OcpiSessionStatus.Active || session.TransactionUid == null)
                return new OcpiCommandOutcome("UNKNOWN_SESSION", "Unknown or finished session.");
            var chargePointId = await _db.ChargePoints.AsNoTracking().Where(cp => cp.ID == session.ChargePointID).Select(cp => cp.ChargePointId).FirstAsync(cancellationToken);
            return await _executor.StopSessionAsync(chargePointId, session.TransactionUid);
        }

        private async Task<OcpiCommandOutcome?> UnlockConnectorAsync(CommandRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.EvseUid) || string.IsNullOrEmpty(request.ConnectorId))
                throw new OcpiValidationException("evse_uid and connector_id are required.");
            var evse = await ResolveEvseAsync(request.LocationId, request.EvseUid, cancellationToken);
            if (evse == null) return null;
            var connector = evse.Value.Connectors.FirstOrDefault(c => OcpiMapper.ConnectorId(c) == request.ConnectorId);
            if (connector == null) return null;
            return await _executor.UnlockConnectorAsync(evse.Value.ChargePoint.ChargePointId, evse.Value.EvseId, connector.ConnectorID ?? 1);
        }

        private async Task<OcpiCommandOutcome?> ReserveNowAsync(OcpiParty party, CommandRequestDto request, CancellationToken cancellationToken)
        {
            var dto = request.Token ?? throw new OcpiValidationException("token is required.");
            if (dto.Uid == null || dto.Type == null || dto.CountryCode == null || dto.PartyId == null)
                throw new OcpiValidationException("token is incomplete.");
            if (string.IsNullOrWhiteSpace(request.ReservationId) || request.ReservationId.Length > 36 || request.ExpiryDate == null)
                throw new OcpiValidationException("reservation_id and expiry_date are required.");
            var evse = await ResolveEvseAsync(request.LocationId, request.EvseUid, cancellationToken);
            if (evse == null) return null;
            dto.LastUpdated ??= DateTime.UtcNow;
            var token = await _tokens.UpsertAsync(party, dto.CountryCode, dto.PartyId, dto.Uid, dto.Type, dto, partial: false, cancellationToken);
            if (!token.Valid) return new OcpiCommandOutcome(OcpiCommandResults.Rejected, "Token is not valid.");

            // Same reservation_id again replaces the existing reservation (OCPI 2.2.1).
            var reservation = await _db.OcpiReservations.FirstOrDefaultAsync(r => r.OcpiPartyID == party.ID && r.ReservationId == request.ReservationId && !r.Cancelled, cancellationToken);
            if (reservation == null)
            {
                reservation = new OcpiReservation { OcpiPartyID = party.ID, ReservationId = request.ReservationId, CreatedAt = DateTime.UtcNow };
                _db.OcpiReservations.Add(reservation);
            }
            reservation.TokenUid = token.Uid;
            reservation.ChargePointID = evse.Value.ChargePoint.ID;
            reservation.EvseId = string.IsNullOrEmpty(request.EvseUid) ? null : evse.Value.EvseId;
            reservation.ExpiryDate = OcpiJson.ToUtc(request.ExpiryDate.Value);
            await _db.SaveChangesAsync(cancellationToken);
            return await _executor.ReserveNowAsync(evse.Value.ChargePoint.ChargePointId, reservation.ID, reservation.ExpiryDate, token.Uid, token.Type, reservation.EvseId);
        }

        private async Task<OcpiCommandOutcome?> CancelReservationAsync(OcpiParty party, CommandRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ReservationId)) throw new OcpiValidationException("reservation_id is required.");
            var reservation = await _db.OcpiReservations.FirstOrDefaultAsync(r => r.OcpiPartyID == party.ID && r.ReservationId == request.ReservationId && !r.Cancelled, cancellationToken);
            if (reservation == null) return new OcpiCommandOutcome(OcpiCommandResults.UnknownReservation);
            var chargePointId = await _db.ChargePoints.IgnoreQueryFilters().AsNoTracking()
                .Where(cp => cp.ID == reservation.ChargePointID).Select(cp => cp.ChargePointId).FirstAsync(cancellationToken);
            var outcome = await _executor.CancelReservationAsync(chargePointId, reservation.ID);
            if (outcome.Result == OcpiCommandResults.Accepted) reservation.Cancelled = true;
            return outcome;
        }
    }
}
