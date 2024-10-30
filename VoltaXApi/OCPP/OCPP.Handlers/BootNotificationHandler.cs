using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class BootNotificationHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;

    public BootNotificationHandler(
      ILoggerFactory loggerFactory
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(BootNotificationHandler));
    }


    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut)
    {
      string? errorCode = null;
      string? bootReason = null;
      try
      {
        var settings = new JsonSerializerSettings
        {
          Converters = new List<JsonConverter> { new StringEnumConverter() }
        };
        _logger.LogTrace("Processing boot notification...");
        BootNotificationRequest bootNotificationRequest = JsonConvert.DeserializeObject<BootNotificationRequest>(msgIn.JsonPayload);
        _logger.LogTrace("BootNotification => Message deserialized");

        bootReason = bootNotificationRequest?.Reason.ToString();
        _logger.LogInformation("BootNotification => Reason={0}", bootReason);

        BootNotificationResponse bootNotificationResponse = new BootNotificationResponse();
        bootNotificationResponse.CurrentTime = DateTime.Now;
        bootNotificationResponse.Interval = 300;

        bootNotificationResponse.CustomData = new CustomDataType();
        bootNotificationResponse.CustomData.VendorId = OCPPHelper.VendorId;

        // if (ChargePointStatus != null)
        // {
        //   // Known charge station => accept
        //   bootNotificationResponse.Status = RegistrationStatusEnumType.Accepted;
        // }
        // else
        // {
        //   // Unknown charge station => reject
        //   bootNotificationResponse.Status = RegistrationStatusEnumType.Rejected;
        // }

        msgOut.JsonPayload = JsonConvert.SerializeObject(bootNotificationResponse, settings);
        _logger.LogTrace("BootNotification => Response serialized");
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "BootNotification => Exception: {0}", exp.Message);
        errorCode = ErrorCodes.FormationViolation;
      }

      // WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, bootReason, errorCode);
      return errorCode;
    }
  }
}