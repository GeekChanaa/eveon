using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.Ocpi.Services
{
    // OCPI CommandResultType values.
    public static class OcpiCommandResults
    {
        public const string Accepted = "ACCEPTED";
        public const string CanceledReservation = "CANCELED_RESERVATION";
        public const string EvseOccupied = "EVSE_OCCUPIED";
        public const string EvseInoperative = "EVSE_INOPERATIVE";
        public const string Failed = "FAILED";
        public const string NotSupported = "NOT_SUPPORTED";
        public const string Rejected = "REJECTED";
        public const string Timeout = "TIMEOUT";
        public const string UnknownReservation = "UNKNOWN_RESERVATION";
    }

    public sealed record OcpiCommandOutcome(string Result, string? Message = null);

    // Sends OCPI commands to the charger. Kept behind an interface so the OCPP sending layer can change.
    public interface IOcpiCommandExecutor
    {
        Task<OcpiCommandOutcome> StartSessionAsync(string chargePointId, int evseId, string idToken, string tokenType, int remoteStartId);
        Task<OcpiCommandOutcome> StopSessionAsync(string chargePointId, string transactionId);
        Task<OcpiCommandOutcome> UnlockConnectorAsync(string chargePointId, int evseId, int connectorId);
        Task<OcpiCommandOutcome> ReserveNowAsync(string chargePointId, int reservationId, DateTime expiry, string idToken, string tokenType, int? evseId);
        Task<OcpiCommandOutcome> CancelReservationAsync(string chargePointId, int reservationId);
    }

    // Sends through EVDriverService (routed to 1.6 or 2.0.1) and maps the charger's real answer to OCPI.
    public class OcppOcpiCommandExecutor : IOcpiCommandExecutor
    {
        private readonly IEVDriverService _evDriverService;
        private readonly ChargePointStatusManagerService _statusManager;
        private readonly ILogger<OcppOcpiCommandExecutor> _logger;

        public OcppOcpiCommandExecutor(IEVDriverService evDriverService, ChargePointStatusManagerService statusManager, ILogger<OcppOcpiCommandExecutor> logger)
        {
            _evDriverService = evDriverService;
            _statusManager = statusManager;
            _logger = logger;
        }

        public static IdTokenEnumType IdTokenType(string ocpiType) => ocpiType switch
        {
            "RFID" => IdTokenEnumType.ISO14443,
            _ => IdTokenEnumType.Central
        };

        public Task<OcpiCommandOutcome> StartSessionAsync(string chargePointId, int evseId, string idToken, string tokenType, int remoteStartId) =>
            RunAsync(chargePointId, "START_SESSION", () => _evDriverService.RequestStartTransaction(chargePointId, new RequestStartTransactionRequest
            {
                IdToken = new IdTokenType { IdToken = idToken, Type = IdTokenType(tokenType) },
                EvseId = evseId,
                RemoteStartId = remoteStartId
            }), r => r.Status.ToString());

        public Task<OcpiCommandOutcome> StopSessionAsync(string chargePointId, string transactionId) =>
            RunAsync(chargePointId, "STOP_SESSION", () => _evDriverService.RequestStopTransaction(chargePointId,
                new RequestStopTransactionRequest { TransactionId = transactionId }), r => r.Status.ToString());

        public Task<OcpiCommandOutcome> UnlockConnectorAsync(string chargePointId, int evseId, int connectorId) =>
            RunAsync(chargePointId, "UNLOCK_CONNECTOR", () => _evDriverService.UnlockConnector(chargePointId,
                new UnlockConnectorRequest { EvseId = evseId, ConnectorId = connectorId }), r => r.Status.ToString());

        public Task<OcpiCommandOutcome> ReserveNowAsync(string chargePointId, int reservationId, DateTime expiry, string idToken, string tokenType, int? evseId) =>
            RunAsync(chargePointId, "RESERVE_NOW", () => _evDriverService.ReserveNow(chargePointId, new ReserveNowRequest
            {
                Id = reservationId,
                ExpiryDateTime = expiry,
                IdToken = new IdTokenType { IdToken = idToken, Type = IdTokenType(tokenType) },
                EvseId = evseId
            }), r => r.Status.ToString());

        public Task<OcpiCommandOutcome> CancelReservationAsync(string chargePointId, int reservationId) =>
            RunAsync(chargePointId, "CANCEL_RESERVATION", () => _evDriverService.CancelReservation(chargePointId,
                new CancelReservationRequest { ReservationId = reservationId }), r => r.Status.ToString());

        private async Task<OcpiCommandOutcome> RunAsync<TResponse>(string chargePointId, string command,
            Func<Task<TResponse>> send, Func<TResponse, string> statusOf)
        {
            if (!_statusManager.ChargePointExists(chargePointId))
                return new OcpiCommandOutcome(OcpiCommandResults.EvseInoperative, "Charge point is offline.");
            try
            {
                var status = statusOf(await send());
                return new OcpiCommandOutcome(MapStatus(status), status == "Accepted" || status == "Unlocked" ? null : $"Charge point answered {status}.");
            }
            catch (TimeoutException)
            {
                return new OcpiCommandOutcome(OcpiCommandResults.Timeout, "The charge point did not answer in time.");
            }
            catch (VoltaXApi.OCPP.Exceptions.OcppProtocolNotSupportedException ex)
            {
                return new OcpiCommandOutcome(OcpiCommandResults.NotSupported, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCPI {Command} failed on {ChargePointId}", command, chargePointId);
                return new OcpiCommandOutcome(OcpiCommandResults.Failed, "The command could not be executed by the charge point.");
            }
        }

        // OCPP response statuses (1.6 and 2.0.1 names) -> OCPI CommandResultType.
        public static string MapStatus(string ocppStatus) => ocppStatus switch
        {
            "Accepted" or "Unlocked" => OcpiCommandResults.Accepted,
            "Occupied" or "OngoingAuthorizedTransaction" => OcpiCommandResults.EvseOccupied,
            "Faulted" or "Unavailable" or "UnknownConnector" or "UnlockFailed" or "NotSupported" => OcpiCommandResults.EvseInoperative,
            _ => OcpiCommandResults.Rejected,
        };
    }
}
