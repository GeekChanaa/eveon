using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class LogStatusNotificationHandler : IOCPPRequestHandler
  {
      public string HandleLogStatusNotification(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;

            Logger.LogTrace("Processing LogStatusNotification...");
            LogStatusNotificationResponse logStatusNotificationResponse = new LogStatusNotificationResponse();
            logStatusNotificationResponse.CustomData = new CustomDataType();
            logStatusNotificationResponse.CustomData.VendorId = VendorId;

            string status = null;

            try
            {
                LogStatusNotificationRequest logStatusNotificationRequest = JsonConvert.DeserializeObject<LogStatusNotificationRequest>(msgIn.JsonPayload);
                Logger.LogTrace("LogStatusNotification => Message deserialized");


                if (ChargePointStatus != null)
                {
                    // Known charge station
                    status = logStatusNotificationRequest.Status.ToString();
                    Logger.LogInformation("LogStatusNotification => Status={0}", status);
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(logStatusNotificationResponse);
                Logger.LogTrace("LogStatusNotification => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "LogStatusNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, status, errorCode);
            return errorCode;
        }
  }
}