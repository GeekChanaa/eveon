

using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class MonitoringService : IMonitoringService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public MonitoringService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

  // POST /ocpp/monitoring/setVariableMonitoring
  public async Task SetVariableMonitoring(string chargePointID, SetVariableMonitoringRequest request)
  {
      OCPPMessage msg = new OCPPMessage
      {
          MessageType = "2",
          UniqueId = Guid.NewGuid().ToString("N"),
          Action = "SetVariableMonitoring",
          JsonPayload = JsonConvert.SerializeObject(request)
      };
      await _messageProcessor.SendMessage(msg, chargePointID);
  }
  public async Task ClearVariableMonitoring(string chargePointID, ClearVariableMonitoringRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "ClearVariableMonitoring",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task SetMonitoringLevel(string chargePointID, SetMonitoringLevelRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "SetMonitoringLevel",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task SetMonitoringBase(string chargePointID, SetMonitoringBaseRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "SetMonitoringBase",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task SetVariables(string chargePointID, SetVariablesRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "SetVariables",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task GetVariables(string chargePointID, GetVariablesRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetVariables",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
  // GET /data/monitoring/systemConfig
  // PUT /data/monitoring/systemConfig
  }
}