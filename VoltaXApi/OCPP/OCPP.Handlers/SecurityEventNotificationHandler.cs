using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class SecurityEventNotificationHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;

    public SecurityEventNotificationHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(SecurityEventNotificationHandler));
      _msgLogRepo = messageLogRepository;
    }


    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
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

        SecurityEventNotificationResponse securityEventNotificationResponse = new SecurityEventNotificationResponse();

        securityEventNotificationResponse.CustomData = new CustomDataType();
        securityEventNotificationResponse.CustomData.VendorId = OCPPHelper.VendorId;
        
        msgOut.JsonPayload = JsonConvert.SerializeObject(securityEventNotificationResponse, settings);
        _logger.LogTrace("BootNotification => Response serialized");
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "BootNotification => Exception: {0}", exp.Message);
        errorCode = ErrorCodes.FormationViolation;
      }

      await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, bootReason, errorCode);
      return errorCode;
    }
  }
}