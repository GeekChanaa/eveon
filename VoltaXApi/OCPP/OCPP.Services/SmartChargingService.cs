using Newtonsoft.Json;
using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class SmartChargingService : ISmartChargingService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    private readonly OCPPMessageFactory _messageFactory;
    
    public SmartChargingService(OCPPMessageProcessor messageProcessor)
    {
      _messageProcessor = messageProcessor;
      _messageFactory = new OCPPMessageFactory();
    }
    
    public async Task ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request)
    {
      var msg = _messageFactory.CreateMessage("ClearChargingProfile", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task GetChargingProfiles(string chargePointID, GetChargingProfilesRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetChargingProfiles", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task SetChargingProfile(string chargePointID, SetChargingProfileRequest request)
    {
      var msg = _messageFactory.CreateMessage("SetChargingProfile", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task ClearedChargingLimit(string chargePointID, ClearedChargingLimitRequest request)
    {
      var msg = _messageFactory.CreateMessage("ClearedChargingLimit", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
    
    public async Task GetCompositeSchedule(string chargePointID, GetCompositeScheduleRequest request)
    {
      var msg = _messageFactory.CreateMessage("GetCompositeSchedule", request);
      await _messageProcessor.SendMessage(msg, chargePointID);
    }
  }
}