using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class LogStatusNotificationHandler : IOCPPRequestHandler
    {
        private readonly IMessageLogRepository _msgLogRepo;
        private readonly ILogger _logger;
        public LogStatusNotificationHandler(
          ILoggerFactory loggerFactory,
          IMessageLogRepository messageLogRepository
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(LogStatusNotificationHandler));
            _msgLogRepo = messageLogRepository;
        }


        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string errorCode = null;

            _logger.LogTrace("Processing LogStatusNotification...");
            LogStatusNotificationResponse logStatusNotificationResponse = new LogStatusNotificationResponse();
            logStatusNotificationResponse.CustomData = new CustomDataType();
            logStatusNotificationResponse.CustomData.VendorId = OCPPHelper.VendorId;

            string status = null;

            try
            {
                LogStatusNotificationRequest logStatusNotificationRequest = JsonConvert.DeserializeObject<LogStatusNotificationRequest>(msgIn.JsonPayload);
                _logger.LogTrace("LogStatusNotification => Message deserialized");


                if (chargePointStatus != null)
                {
                    // Known charge station
                    status = logStatusNotificationRequest.Status.ToString();
                    _logger.LogInformation("LogStatusNotification => Status={0}", status);
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(logStatusNotificationResponse);
                _logger.LogTrace("LogStatusNotification => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "LogStatusNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, status, errorCode);
            return errorCode;
        }
    }
}