using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class MonitoringService : IMonitoringService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    private readonly OCPPMessageFactory _messageFactory;
    
    public MonitoringService(OCPPMessageProcessor messageProcessor)
    {
      _messageProcessor = messageProcessor;
      _messageFactory = new OCPPMessageFactory();
    }
    
    // POST /ocpp/monitoring/setVariableMonitoring
    public async Task SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request)
    {
      var msg = _messageFactory.CreateMessage("SetVariableMonitoring", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request)
    {
      var msg = _messageFactory.CreateMessage("ClearVariableMonitoring", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request)
    {
      var msg = _messageFactory.CreateMessage("SetMonitoringLevel", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request)
    {
      var msg = _messageFactory.CreateMessage("SetMonitoringBase", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task SetVariables(string chargePointID, SetVariablesRequest request)
    {
      var msg = _messageFactory.CreateMessage("SetVariables", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task GetVariables(string chargePointID, GetVariablesRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetVariables", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    // GET /data/monitoring/systemConfig
    // PUT /data/monitoring/systemConfig
  }
}