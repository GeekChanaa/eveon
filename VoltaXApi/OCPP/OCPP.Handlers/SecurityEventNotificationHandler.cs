using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
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
      string? eventType = null;
      try
      {
        var request = JsonConvert.DeserializeObject<SecurityEventNotificationRequest>(msgIn.JsonPayload ?? string.Empty);
        eventType = request?.Type;
        _logger.LogWarning("SecurityEventNotification => {ChargePointId} Type={Type} At={Timestamp:o} TechInfo={TechInfo}",
          chargePointStatus.Id, request?.Type, request?.Timestamp, request?.TechInfo);

        var response = new SecurityEventNotificationResponse
        {
          CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId }
        };
        msgOut.JsonPayload = JsonConvert.SerializeObject(response, OCPPMessageFactory.DefaultSettings);
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "SecurityEventNotification => Exception processing request from {ChargePointId}", chargePointStatus.Id);
        errorCode = ErrorCodes.FormationViolation;
      }

      await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, eventType!, errorCode!, msgIn, msgOut);
      return errorCode!;
    }
  }
}
