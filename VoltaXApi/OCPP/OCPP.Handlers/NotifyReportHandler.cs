using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Handlers
{
  public class NotifyReportHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    public NotifyReportHandler(
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
        NotifyReportResponse notifyReportResponse = new NotifyReportResponse();
        notifyReportResponse.CustomData = new CustomDataType();
        notifyReportResponse.CustomData.VendorId = OCPPHelper.VendorId;


        msgOut.JsonPayload = JsonConvert.SerializeObject(notifyReportResponse);
        _logger.LogTrace("NotifyReport => Response serialized");

        await _msgLogRepo.SaveLogMessage(chargePointStatus?.Id, null, msgIn.Action, "Report", errorCode, msgIn, msgOut);
        return errorCode;
    }
  }
}