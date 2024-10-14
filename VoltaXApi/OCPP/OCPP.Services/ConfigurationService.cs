using System.Net.WebSockets;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Core;
using Newtonsoft.Json;

namespace VoltaXApi.OCPP.Services
{
  public class ConfigurationService : IConfigurationService
  {
    private readonly OCPPMessageProcessor _messageProcessor;
    public ConfigurationService(
      OCPPMessageProcessor messageProcessor
    ){
      _messageProcessor = messageProcessor;
    }

    
    public async Task SetNetworkProfile(string ChargePointID,SetNetworkProfileRequest request)
    {
      OCPPMessage  msg = new OCPPMessage{
        MessageType = "2",
        UniqueId =  Guid.NewGuid().ToString("N"),
        Action = "SetNetworkProfile",
        JsonPayload = JsonConvert.SerializeObject(request)
      };
      await _messageProcessor.SendMessage(msg,ChargePointID);
    }
    
    public async Task ClearDisplayMessage(string ChargePointID, ClearDisplayMessageRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "ClearDisplayMessage",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task GetDisplayMessages(string ChargePointID, GetDisplayMessagesRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "GetDisplayMessages",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task PublishFirmware(string ChargePointID, PublishFirmwareRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "PublishFirmware",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task SetDisplayMessage(string ChargePointID, SetDisplayMessageRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "SetDisplayMessage",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task UnpublishFirmware(string ChargePointID, UnpublishFirmwareRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "UnpublishFirmware",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task UpdateFirmware(string ChargePointID, UpdateFirmwareRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "UpdateFirmware",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task Reset(string ChargePointID, ResetRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "Reset",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task ChangeAvailability(string ChargePointID, ChangeAvailabilityRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "ChangeAvailability",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    public async Task TriggerMessage(string ChargePointID, TriggerMessageRequest request)
    {
        OCPPMessage msg = new OCPPMessage
        {
            MessageType = "2",
            UniqueId = Guid.NewGuid().ToString("N"),
            Action = "TriggerMessage",
            JsonPayload = JsonConvert.SerializeObject(request)
        };
        await _messageProcessor.SendMessage(msg, ChargePointID);
    }

    
    //GET /data/configuration/systemConfig
    
    //PUT /data/configuration/systemConfig
  }
}