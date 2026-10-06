using System.Globalization;
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
    /// <summary>NotifyEVChargingSchedule (2.0.1, ISO 15118): the EV's planned schedule, stored with its charging needs.</summary>
    public class NotifyEVChargingScheduleHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly SmartChargingInboundStore _store;
        private readonly ILogger _logger;

        public NotifyEVChargingScheduleHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository,
            SmartChargingInboundStore store)
        {
            _logger = loggerFactory.CreateLogger(typeof(NotifyEVChargingScheduleHandler));
            _msgLogRepo = messageLogRepository;
            _store = store;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string? errorCode = null;
            var periods = new StringBuilder();
            var evseId = 0;
            var response = new NotifyEVChargingScheduleResponse
            {
                CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId },
                Status = GenericStatusEnumType.Accepted
            };

            try
            {
                var request = JsonConvert.DeserializeObject<NotifyEVChargingScheduleRequest>(msgIn.JsonPayload ?? string.Empty);
                if (request == null)
                {
                    errorCode = ErrorCodes.FormationViolation;
                }
                else
                {
                    evseId = request.EvseId;
                    var schedule = request.ChargingSchedule;
                    if (schedule?.ChargingSchedulePeriod != null
                        && DateTimeOffset.TryParse(request.TimeBase, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timeBase))
                    {
                        foreach (var period in schedule.ChargingSchedulePeriod)
                        {
                            if (periods.Length > 0) periods.Append(" | ");
                            periods.Append($"{timeBase.AddSeconds(period.StartPeriod):O}: {period.Limit}{schedule.ChargingRateUnit}");
                            if (period.NumberPhases > 0) periods.Append($" ({period.NumberPhases} Phases)");
                        }
                    }
                    _logger.LogInformation("NotifyEVChargingSchedule => {ChargePointId} EVSE {EvseId} {Periods}", chargePointStatus.Id, evseId, periods);
                    await _store.SaveEvChargingScheduleAsync(chargePointStatus.Id, request);
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "NotifyEVChargingSchedule => Exception processing request from {ChargePointId}", chargePointStatus.Id);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, evseId, msgIn.Action, periods.ToString(), errorCode!, msgIn, msgOut);
            return errorCode!;
        }
    }
}
