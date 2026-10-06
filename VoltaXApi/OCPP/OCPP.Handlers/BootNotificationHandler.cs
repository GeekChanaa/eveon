using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.OCPP.Handlers
{
  public class BootNotificationHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly ChargePointBootService _bootService;

    public BootNotificationHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository,
      ChargePointBootService bootService
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(BootNotificationHandler));
      _msgLogRepo = messageLogRepository;
      _bootService = bootService;
    }


    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
      string? errorCode = null;
      string? bootReason = null;
      try
      {
        var bootNotificationRequest = JsonConvert.DeserializeObject<BootNotificationRequest>(msgIn.JsonPayload ?? string.Empty);
        if (bootNotificationRequest?.ChargingStation == null)
        {
          _logger.LogWarning("BootNotification => Invalid payload from {ChargePointId}", chargePointStatus.Id);
          errorCode = ErrorCodes.FormationViolation;
        }
        else
        {
          bootReason = bootNotificationRequest.Reason.ToString();
          _logger.LogInformation("BootNotification => {ChargePointId} Reason={Reason}", chargePointStatus.Id, bootReason);

          var decision = await _bootService.RegisterBootAsync(chargePointStatus, bootNotificationRequest);
          var bootNotificationResponse = new BootNotificationResponse
          {
            CurrentTime = DateTime.UtcNow,
            Interval = decision.Interval,
            Status = decision.Status,
            CustomData = new CustomDataType { VendorId = OCPPHelper.VendorId }
          };

          _logger.LogInformation("BootNotification => {ChargePointId} answered {Status}", chargePointStatus.Id, bootNotificationResponse.Status);
          msgOut.JsonPayload = JsonConvert.SerializeObject(bootNotificationResponse, OCPPMessageFactory.DefaultSettings);
        }
      }
      catch (Exception exp)
      {
        _logger.LogError(exp, "BootNotification => Exception processing request from {ChargePointId}", chargePointStatus.Id);
        errorCode = ErrorCodes.FormationViolation;
      }

      await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, bootReason, errorCode, msgIn, msgOut);
      return errorCode!;
    }
  }
}
