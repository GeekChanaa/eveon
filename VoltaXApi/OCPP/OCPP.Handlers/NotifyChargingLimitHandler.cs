using System.Text;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Models;
using VoltaXApi.SmartCharging;

namespace VoltaXApi.OCPP.Handlers
{
    /// <summary>NotifyChargingLimit (2.0.1): an external system (EMS, SO...) limits the charger; stored as ChargingStationExternalConstraints.</summary>
    public class NotifyChargingLimitHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly SmartChargingInboundStore _store;
        private readonly ILogger _logger;

        public NotifyChargingLimitHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            SmartChargingInboundStore store)
        {
            _logger = loggerFactory.CreateLogger(typeof(NotifyChargingLimitHandler));
            _msgLogRepo = messageLogRepository;
            _store = store;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            string? source = null;
            var periods = new StringBuilder();
            var evseId = 0;
            var response = new NotifyChargingLimitResponse
            {
                CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId }
            };

            try
            {
                var request = JsonConvert.DeserializeObject<NotifyChargingLimitRequest>(msgIn.JsonPayload ?? string.Empty);
                if (request == null)
                {
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    source = request.ChargingLimit?.ChargingLimitSource.ToString();
                    evseId = request.EvseId;
                    foreach (var schedule in request.ChargingSchedule ?? new List<ChargingScheduleType>())
                    {
                        foreach (var period in schedule.ChargingSchedulePeriod ?? new List<ChargingSchedulePeriodType>())
                        {
                            if (periods.Length > 0) periods.Append(" | ");
                            periods.Append($"{period.StartPeriod}s: {period.Limit}{schedule.ChargingRateUnit}");
                            if (period.NumberPhases > 0) periods.Append($" ({period.NumberPhases} Phases)");
                        }
                    }
                    _logger.LogInformation("NotifyChargingLimit => {ChargePointId} EVSE {EvseId} {Source}: {Periods}", chargePointStatus.Id, evseId, source, periods);
                    await _store.SaveChargingLimitAsync(chargePointStatus.Id, request);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "NotifyChargingLimit => Exception processing request from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, evseId, msgIn.Action, source ?? periods.ToString(), errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }
}
