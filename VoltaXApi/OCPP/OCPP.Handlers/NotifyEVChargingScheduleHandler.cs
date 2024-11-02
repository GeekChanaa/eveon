using System.Text;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class NotifyEVChargingScheduleHandler : IOCPPRequestHandler
    {
    private readonly IMessageLogRepository _msgLogRepo;
        private readonly ILogger _logger;
        public NotifyEVChargingScheduleHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(NotifyEVChargingScheduleHandler));
        }
        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string errorCode = null;

            _logger.LogTrace("Processing NotifyEVChargingSchedule...");
            NotifyEVChargingScheduleResponse notifyEVChargingScheduleResponse = new NotifyEVChargingScheduleResponse();
            notifyEVChargingScheduleResponse.CustomData = new CustomDataType();
            notifyEVChargingScheduleResponse.CustomData.VendorId = OCPPHelper.VendorId;

            StringBuilder periods = new StringBuilder();
            int connectorId = 0;

            try
            {
                NotifyEVChargingScheduleRequest notifyEVChargingScheduleRequest = JsonConvert.DeserializeObject<NotifyEVChargingScheduleRequest>(msgIn.JsonPayload);
                _logger.LogTrace("NotifyEVChargingSchedule => Message deserialized");


                if (chargePointStatus != null)
                {
                    // Known charge station
                    if (notifyEVChargingScheduleRequest.ChargingSchedule != null)
                    {
                        if (notifyEVChargingScheduleRequest.ChargingSchedule?.ChargingSchedulePeriod != null)
                        {
                            // Concat all periods and write them in messag log...

                            DateTimeOffset timeBase = DateTime.Parse(notifyEVChargingScheduleRequest.TimeBase);
                            foreach (ChargingSchedulePeriodType period in notifyEVChargingScheduleRequest.ChargingSchedule?.ChargingSchedulePeriod)
                            {
                                if (periods.Length > 0)
                                {
                                    periods.Append(" | ");
                                }

                                DateTimeOffset time = timeBase.AddSeconds(period.StartPeriod);
                                periods.Append(string.Format("{0}: {1}{2}", time.ToString("O"), period.Limit, notifyEVChargingScheduleRequest.ChargingSchedule.ChargingRateUnit.ToString()));

                                if (period.NumberPhases > 0)
                                {
                                    periods.Append(string.Format(" ({0} Phases)", period.NumberPhases));
                                }
                            }
                        }
                    }
                    connectorId = notifyEVChargingScheduleRequest.EvseId;
                    _logger.LogInformation("NotifyEVChargingSchedule => {0}", periods.ToString());
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(notifyEVChargingScheduleResponse);
                _logger.LogTrace("NotifyEVChargingSchedule => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "NotifyEVChargingSchedule => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, periods.ToString(), errorCode);
            return errorCode;
        }
    }
}