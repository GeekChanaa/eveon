using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class BootNotificationHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly IChargePointRepository _chargePointRepository;

    public BootNotificationHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository,
      IChargePointRepository chargePointRepository
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(BootNotificationHandler));
      _msgLogRepo = messageLogRepository;
      _chargePointRepository = chargePointRepository;
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

        // Updating ChargePoint Informations based on the bootnotificationRequest
        _logger.LogTrace("Updating Informations for ChargePoint : " + chargePointStatus.Id);
        var chargePoint = await _chargePointRepository.GetChargePointByChargePointIDAsync(chargePointStatus.Id);
        chargePoint.Model = bootNotificationRequest.ChargingStation.Model;
        chargePoint.SerialNumber = bootNotificationRequest.ChargingStation.SerialNumber;
        chargePoint.VendorName = bootNotificationRequest.ChargingStation.VendorName;
        await _chargePointRepository.Update(chargePoint);

        
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

      await _msgLogRepo.SaveLogMessage(chargePointStatus.Id, null, msgIn.Action, bootReason, errorCode, msgIn, msgOut);
      return errorCode;
    }
  }
}