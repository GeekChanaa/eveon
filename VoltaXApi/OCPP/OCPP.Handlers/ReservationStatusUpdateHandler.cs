using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>OCPP 2.0.1 ReservationStatusUpdate: the charger expired or removed a reservation.</summary>
    public class ReservationStatusUpdateHandler : IOCPPRequestHandler
    {
        private readonly ReservationService _reservations;
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ILogger<ReservationStatusUpdateHandler> _logger;

        public ReservationStatusUpdateHandler(ReservationService reservations, IMessageLogRepository messageLogRepository, ILogger<ReservationStatusUpdateHandler> logger)
        {
            _reservations = reservations;
            _msgLogRepo = messageLogRepository;
            _logger = logger;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            var request = JsonConvert.DeserializeObject<ReservationStatusUpdateRequest>(msgIn.JsonPayload ?? string.Empty);
            if (request == null)
            {
                await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, null, ErrorCodes.FormationViolation, msgIn, msgOut);
                return ErrorCodes.FormationViolation;
            }

            var status = request.ReservationUpdateStatus == ReservationUpdateStatusEnumType.Expired
                ? ReservationStatusEnum.Expired
                : ReservationStatusEnum.Cancelled;
            _logger.LogInformation("ReservationStatusUpdate => {ChargePointId} reservation {ReservationId}: {Status}",
                chargePointStatus.Id, request.ReservationId, request.ReservationUpdateStatus);
            await _reservations.UpdateActiveAsync(chargePointStatus.Id, request.ReservationId, status);

            msgOut.JsonPayload = "{}";
            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action,
                $"Reservation {request.ReservationId} => {request.ReservationUpdateStatus}", null!, msgIn, msgOut);
            return null!;
        }
    }
}
