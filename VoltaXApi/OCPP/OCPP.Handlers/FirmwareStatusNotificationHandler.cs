using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
    public class FirmwareStatusNotificationHandler : IOCPPRequestHandler
    {
        private readonly ILogger _logger;
        private readonly IMessageLogRepository _msgLogRepo;
        public FirmwareStatusNotificationHandler(
          ILoggerFactory loggerFactory,
          IMessageLogRepository messageLogRepository
        )
        {
            _logger = loggerFactory.CreateLogger(typeof(FirmwareStatusNotificationHandler));
            _msgLogRepo = messageLogRepository;
        }

        public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
        {
            string errorCode = null;

            _logger.LogTrace("Processing FirmwareStatusNotification...");
            FirmwareStatusNotificationResponse firmwareStatusNotificationResponse = new FirmwareStatusNotificationResponse();
            firmwareStatusNotificationResponse.CustomData = new CustomDataType();
            firmwareStatusNotificationResponse.CustomData.VendorId = OCPPHelper.VendorId;

            string status = null;

            try
            {
                FirmwareStatusNotificationRequest firmwareStatusNotificationRequest = JsonConvert.DeserializeObject<FirmwareStatusNotificationRequest>(msgIn.JsonPayload);
                _logger.LogTrace("FirmwareStatusNotification => Message deserialized");


                if (chargePointStatus != null)
                {
                    // Known charge station
                    status = firmwareStatusNotificationRequest.Status.ToString();
                    _logger.LogInformation("FirmwareStatusNotification => Status={0}", status);
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(firmwareStatusNotificationResponse);
                _logger.LogTrace("FirmwareStatusNotification => Response serialized");
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "FirmwareStatusNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, status, errorCode, msgIn, msgOut);
            return errorCode;
        }
    }
}