using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Services;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>
    /// NotifyEVChargingNeeds (2.0.1, ISO 15118): stored, then answered Accepted when the station is load balanced
    /// (a TxProfile follows from the rebalance, using the EV's maximum current) and Rejected otherwise
    /// (no schedule will be provided, the charger keeps its own).
    /// </summary>
    public class NotifyEVChargingNeedsHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly SmartChargingInboundStore _store;
        private readonly VoltaXApiDbContext _db;
        private readonly ILoadBalancingTrigger _loadBalancing;
        private readonly ILogger<NotifyEVChargingNeedsHandler> _logger;

        public NotifyEVChargingNeedsHandler(
            IMessageLogRepository messageLogRepository,
            SmartChargingInboundStore store,
            VoltaXApiDbContext db,
            ILoadBalancingTrigger loadBalancing,
            ILogger<NotifyEVChargingNeedsHandler> logger)
        {
            _msgLogRepo = messageLogRepository;
            _store = store;
            _db = db;
            _loadBalancing = loadBalancing;
            _logger = logger;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            int? evseId = null;
            var response = new NotifyEVChargingNeedsResponse
            {
                CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId },
                Status = NotifyEVChargingNeedsStatusEnumType.Rejected
            };

            try
            {
                var request = JsonConvert.DeserializeObject<NotifyEVChargingNeedsRequest>(msgIn.JsonPayload ?? string.Empty);
                if (request?.ChargingNeeds == null)
                {
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    evseId = request.EvseId;
                    var stationId = await _store.SaveEvChargingNeedsAsync(chargePointStatus.Id, request);
                    if (stationId != null && await _db.StationLoadLimits.AnyAsync(l => l.ChargingStationID == stationId && l.Enabled))
                    {
                        response.Status = NotifyEVChargingNeedsStatusEnumType.Accepted;
                        _loadBalancing.RequestRebalance(stationId.Value);
                    }
                    _logger.LogInformation("NotifyEVChargingNeeds => {ChargePointId} EVSE {EvseId} {Transfer}, departure {Departure}: {Status}",
                        chargePointStatus.Id, evseId, request.ChargingNeeds.RequestedEnergyTransfer, request.ChargingNeeds.DepartureTime, response.Status);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "NotifyEVChargingNeeds => Exception processing request from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, evseId, msgIn.Action, response.Status.ToString(), errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }
}
