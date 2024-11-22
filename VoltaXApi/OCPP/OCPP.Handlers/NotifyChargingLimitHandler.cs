using System.Text;
using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class NotifyChargingLimitHandler : IOCPPRequestHandler
    {
    private readonly IMessageLogRepository _msgLogRepo;
        private readonly ILogger _logger;
        public NotifyChargingLimitHandler(
            ILoggerFactory loggerFactory,
            IMessageLogRepository messageLogRepository
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(NotifyChargingLimitHandler));
        }


        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string errorCode = null;

            _logger.LogTrace("Processing NotifyChargingLimit...");
            NotifyChargingLimitResponse notifyChargingLimitResponse = new NotifyChargingLimitResponse();
            notifyChargingLimitResponse.CustomData = new CustomDataType();
            notifyChargingLimitResponse.CustomData.VendorId = OCPPHelper.VendorId;

            string source = null;
            StringBuilder periods = new StringBuilder();
            int connectorId = 0;

            try
            {
                NotifyChargingLimitRequest notifyChargingLimitRequest = JsonConvert.DeserializeObject<NotifyChargingLimitRequest>(msgIn.JsonPayload);
                _logger.LogTrace("NotifyChargingLimit => Message deserialized");


                if (chargePointStatus != null)
                {
                    // Known charge station
                    source = notifyChargingLimitRequest.ChargingLimit?.ChargingLimitSource.ToString();
                    if (notifyChargingLimitRequest.ChargingSchedule != null)
                    {
                        foreach (ChargingScheduleType schedule in notifyChargingLimitRequest.ChargingSchedule)
                        {
                            if (schedule.ChargingSchedulePeriod != null)
                            {
                                foreach (ChargingSchedulePeriodType period in schedule.ChargingSchedulePeriod)
                                {
                                    if (periods.Length > 0)
                                    {
                                        periods.Append(" | ");
                                    }

                                    periods.Append(string.Format("{0}s: {1}{2}", period.StartPeriod, period.Limit, schedule.ChargingRateUnit));

                                    if (period.NumberPhases > 0)
                                    {
                                        periods.Append(string.Format(" ({0} Phases)", period.NumberPhases));
                                    }
                                }
                            }
                        }
                    }
                    connectorId = notifyChargingLimitRequest.EvseId;
                    _logger.LogInformation("NotifyChargingLimit => {0}", periods);
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(notifyChargingLimitResponse);
                _logger.LogTrace("NotifyChargingLimit => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "NotifyChargingLimit => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, connectorId, msgIn.Action, source, errorCode, msgIn, msgOut);
            return errorCode;
        }
    }
}