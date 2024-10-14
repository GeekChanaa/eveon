
using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;


namespace VoltaXApi.OCPP.Services
{
  public class SmartChargingService : ISmartChargingService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public SmartChargingService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

    public async Task ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "ClearChargingProfile",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task GetChargingProfiles(string chargePointID, GetChargingProfilesRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetChargingProfiles",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task SetChargingProfile(string chargePointID, SetChargingProfileRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "SetChargingProfile",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task ClearedChargingLimit(string chargePointID, ClearedChargingLimitRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "ClearedChargingLimit",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }

    public async Task GetCompositeSchedule(string chargePointID, GetCompositeScheduleRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetCompositeSchedule",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, chargePointID);
    }
  }
}