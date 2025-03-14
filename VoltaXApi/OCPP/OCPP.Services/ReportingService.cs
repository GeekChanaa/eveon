using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ReportingService> _logger;
    
    public ReportingService(OCPPMessageProcessor messageProcessor, ILogger<ReportingService> logger)
    {
      _messageProcessor = messageProcessor;
      _messageFactory = new OCPPMessageFactory();
      _logger = logger;
    }
    
    public async Task GetBaseReport(string chargePointID, GetBaseReportRequest request)
    {
      _logger.LogInformation("Starting GetBaseReport for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("GetBaseReport", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed GetBaseReport for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task GetReport(string chargePointID, GetReportRequest request)
    {
      _logger.LogInformation("Starting GetReport for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("GetReport", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed GetReport for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task GetMonitoringReport(string chargePointID, GetMonitoringReportRequest request)
    {
      _logger.LogInformation("Starting GetMonitoringReport for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("GetMonitoringReport", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed GetMonitoringReport for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task GetLog(string chargePointID, GetLogRequest request)
    {
      _logger.LogInformation("Starting GetLog for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("GetLog", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed GetLog for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task CustomerInformation(string chargePointID, CustomerInformationRequest request)
    {
      _logger.LogInformation("Starting CustomerInformation for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("CustomerInformation", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed CustomerInformation for ChargePoint: {ChargePointID}", chargePointID);
    }
  }
}
