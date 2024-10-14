
using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class ReportingService : IReportingService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public ReportingService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

    public async Task GetBaseReport(string chargePointID, GetBaseReportRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetBaseReport",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task GetReport(string chargePointID, GetReportRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetReport",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetMonitoringReport",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task GetLog(string chargePointID, GetLogRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetLog",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task CustomerInformation(string chargePointID, CustomerInformationRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "CustomerInformation",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    //GET    /data/reporting/systemConfig
    //PUT    /data/reporting/systemConfig
  }
}