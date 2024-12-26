using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Handlers
{
  public class NotifyReportHandler : IOCPPRequestHandler
  {

    private readonly ILogger _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly IConnectorService _connectorService;
    private readonly IConnectorStatusService _connectorStatusService;
    public NotifyReportHandler(
      ILoggerFactory loggerFactory,
      IMessageLogRepository messageLogRepository,
      IConnectorService connectorService,
      IConnectorStatusService connectorStatusService
    )
    {
        _logger = loggerFactory.CreateLogger(typeof(HeartBeatHandler));
        _msgLogRepo = messageLogRepository;
        _connectorService = connectorService;
        _connectorStatusService = connectorStatusService;
    }

    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
        string errorCode = null;

        _logger.LogTrace("Processing NotifyReport...");
        NotifyReportRequest notifyReportRequest = JsonConvert.DeserializeObject<NotifyReportRequest>(msgIn.JsonPayload);

        var connectors = notifyReportRequest.ReportData.Where(d => d.Component.Name == "Connector" && d.Variable.Name == "AvailabilityState").ToList();
        if(connectors.Count>0){
          // Refreshing Connectors based on the connector
          await this._connectorService.RefreshChargePointConnectors(connectors,chargePointStatus.Id);

          // Refreshing Connector Statuses based on the request
          await this._connectorStatusService.RefreshConnectorStatuses(connectors,chargePointStatus.Id);
        }



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