using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{

  
  public class HeartBeatHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    public HeartBeatHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository
    )
    {
        _logger = loggerFactory.CreateLogger(typeof(HeartBeatHandler));
        _msgLogRepo = messageLogRepository;
    }

    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
        string errorCode = null;

        _logger.LogTrace("Processing heartbeat...");
        HeartbeatResponse heartbeatResponse = new HeartbeatResponse();
        heartbeatResponse.CustomData = new CustomDataType();
        heartbeatResponse.CustomData.VendorId = OCPPHelper.VendorId;

        heartbeatResponse.CurrentTime = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        msgOut.JsonPayload = JsonConvert.SerializeObject(heartbeatResponse);
        _logger.LogTrace("Heartbeat => Response serialized");

        await _msgLogRepo.SaveLogMessage(chargePointStatus?.Id, null, msgIn.Action, null, errorCode);
        return errorCode;
    }
  }
}