using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
  public class BootNotificationHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly IChargePointRepository _chargePointRepository;
    private readonly IChargePointModelRepository _chargePointModelRepository;
    private readonly IChargePointService _chargePointService;

    public BootNotificationHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository,
      IChargePointRepository chargePointRepository,
      IChargePointModelRepository chargePointModelRepository,
      IChargePointService chargePointService
    )
    {
      _logger = loggerFactory.CreateLogger(typeof(BootNotificationHandler));
      _msgLogRepo = messageLogRepository;
      _chargePointRepository = chargePointRepository;
      _chargePointModelRepository = chargePointModelRepository;
      _chargePointService = chargePointService;
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
        await _chargePointService.SetBootNotificationInfo(chargePointStatus, bootNotificationRequest);


        _logger.LogInformation("BootNotification => Reason={0}", bootReason);
        BootNotificationResponse bootNotificationResponse = new BootNotificationResponse();
        bootNotificationResponse.CurrentTime = DateTime.Now;
        bootNotificationResponse.Interval = 300;

        bootNotificationResponse.CustomData = new CustomDataType
        {
          VendorId = OCPPHelper.VendorId
        };

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