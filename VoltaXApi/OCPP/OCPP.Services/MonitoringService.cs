using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using Microsoft.Extensions.Logging;

namespace VoltaXApi.OCPP.Services
{
  public class MonitoringService : IMonitoringService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    private readonly OCPPMessageFactory _messageFactory;
    private readonly ILogger<MonitoringService> _logger;
    
    public MonitoringService(OCPPMessageProcessor messageProcessor, ILogger<MonitoringService> logger)
    {
      _messageProcessor = messageProcessor;
      _messageFactory = new OCPPMessageFactory();
      _logger = logger;
    }
    
    // POST /ocpp/monitoring/setVariableMonitoring
    public async Task SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request)
    {
      _logger.LogInformation("Starting SetVariableMonitoring for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("SetVariableMonitoring", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed SetVariableMonitoring for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request)
    {
      _logger.LogInformation("Starting ClearVariableMonitoring for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("ClearVariableMonitoring", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed ClearVariableMonitoring for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request)
    {
      _logger.LogInformation("Starting SetMonitoringLevel for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("SetMonitoringLevel", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed SetMonitoringLevel for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request)
    {
      _logger.LogInformation("Starting SetMonitoringBase for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("SetMonitoringBase", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed SetMonitoringBase for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task SetVariables(string chargePointID, SetVariablesRequest request)
    {
      _logger.LogInformation("Starting SetVariables for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("SetVariables", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed SetVariables for ChargePoint: {ChargePointID}", chargePointID);
    }
    
    public async Task GetVariables(string chargePointID, GetVariablesRequest request)
    {
      _logger.LogInformation("Starting GetVariables for ChargePoint: {ChargePointID}", chargePointID);
      var msg = _messageFactory.CreateMessage("GetVariables", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
      _logger.LogInformation("Completed GetVariables for ChargePoint: {ChargePointID}", chargePointID);
    }
  }
}
