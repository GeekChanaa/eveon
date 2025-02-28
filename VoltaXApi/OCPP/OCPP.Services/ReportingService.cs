using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class ReportingService : IReportingService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    private readonly OCPPMessageFactory _messageFactory;
    
    public ReportingService(OCPPMessageProcessor messageProcessor)
    {
      _messageProcessor = messageProcessor;
      _messageFactory = new OCPPMessageFactory();
    }
    
    public async Task GetBaseReport(string chargePointID, GetBaseReportRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetBaseReport", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task GetReport(string chargePointID, GetReportRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetReport", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetMonitoringReport", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task GetLog(string chargePointID, GetLogRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetLog", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task CustomerInformation(string chargePointID, CustomerInformationRequest request)
    {
      var msg = _messageFactory.CreateMessage("CustomerInformation", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    //GET    /data/reporting/systemConfig
    //PUT    /data/reporting/systemConfig
  }
}