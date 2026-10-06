using Newtonsoft.Json;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using VoltaXApi.Services;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.OCPP.Handlers
{
  public class NotifyReportHandler : IOCPPRequestHandler
  {

    private readonly ILogger<NotifyReportHandler> _logger;
    private readonly IMessageLogRepository _msgLogRepo;
    private readonly IConnectorService _connectorService;
    private readonly IConnectorStatusService _connectorStatusService;
    private readonly IOCPPConfigurationItemRepository _ocppConfigurationItemRepository;
    private readonly ConnectorReportBuffer _connectorReports;
    private readonly ReportCompletionTracker _reportTracker;
    public NotifyReportHandler(
      IMessageLogRepository messageLogRepository,
      IConnectorService connectorService,
      IConnectorStatusService connectorStatusService,
      ILogger<NotifyReportHandler> logger,
      IOCPPConfigurationItemRepository ocppConfigurationItemRepository,
      ConnectorReportBuffer connectorReports,
      ReportCompletionTracker reportTracker
    )
    {
        _logger = logger;
        _msgLogRepo = messageLogRepository;
        _connectorService = connectorService;
        _connectorStatusService = connectorStatusService;
        _ocppConfigurationItemRepository = ocppConfigurationItemRepository;
        _connectorReports = connectorReports;
        _reportTracker = reportTracker;
    }

    public async Task<string> Handle(OCPPMessage msgIn, OCPPMessage msgOut, ChargePointStatus chargePointStatus)
    {
        string errorCode = null;

        _logger.LogInformation("Processing NotifyReport...");
        NotifyReportRequest notifyReportRequest = JsonConvert.DeserializeObject<NotifyReportRequest>(msgIn.JsonPayload);

        var connectors = _connectorReports.Add(chargePointStatus.Id, notifyReportRequest);
        if(connectors is { Count: > 0 }){
          // Refreshing Connectors based on the connector
          await this._connectorService.RefreshChargePointConnectors(connectors,chargePointStatus.Id);

          // Refreshing Connector Statuses based on the request
          if (!await this._connectorStatusService.RefreshConnectorStatuses(connectors,chargePointStatus.Id))
              throw new InvalidOperationException("Could not refresh reported connector statuses.");
        }
        
        _logger.LogInformation($"Saving OCPP Configurations for chargepoint : {chargePointStatus.Id}");
        await _ocppConfigurationItemRepository.SaveConfigurationsFromReportAsync(chargePointStatus.Id,notifyReportRequest);
        if (connectors != null) _connectorReports.Complete(chargePointStatus.Id, notifyReportRequest.RequestId);
        _logger.LogInformation($"Finished saving OCPP Configurations for chargepoint : {chargePointStatus.Id}");
        _reportTracker.PartReceived(chargePointStatus.Id, notifyReportRequest.RequestId, notifyReportRequest.Tbc == true);


        NotifyReportResponse notifyReportResponse = new NotifyReportResponse();
        notifyReportResponse.CustomData = new CustomDataType();
        notifyReportResponse.CustomData.VendorId = OCPPHelper.VendorId;


        msgOut.JsonPayload = JsonConvert.SerializeObject(notifyReportResponse, OCPPMessageFactory.DefaultSettings);
        _logger.LogTrace("NotifyReport => Response serialized");

        await _msgLogRepo.SaveLogMessage(chargePointStatus?.Id, null, msgIn.Action, "Report", errorCode, msgIn, msgOut);
        return errorCode;
        
    }
  }
}
