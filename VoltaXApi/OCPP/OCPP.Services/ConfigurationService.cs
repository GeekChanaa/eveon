using System.Net.WebSockets;
using VoltaXApi.OCPP.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Core;
using Newtonsoft.Json;
using OCPP.Core.Server;
using Newtonsoft.Json.Converters;

namespace VoltaXApi.OCPP.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly OCPPMessageProcessor _messageProcessor;
        private readonly OCPPMessageFactory _messageFactory;
        
        public ConfigurationService(OCPPMessageProcessor messageProcessor)
        {
            _messageProcessor = messageProcessor;
            _messageFactory = new OCPPMessageFactory();
        }

        public async Task SetNetworkProfile(string ChargePointID, SetNetworkProfileRequest request)
        {
            var msg = _messageFactory.CreateMessage("SetNetworkProfile", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }
        
        public async Task ClearDisplayMessage(string ChargePointID, ClearDisplayMessageRequest request)
        {
            var msg = _messageFactory.CreateMessage("ClearDisplayMessage", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task GetDisplayMessages(string ChargePointID, GetDisplayMessagesRequest request)
        {
            var msg = _messageFactory.CreateMessage("GetDisplayMessages", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task PublishFirmware(string ChargePointID, PublishFirmwareRequest request)
        {
            var msg = _messageFactory.CreateMessage("PublishFirmware", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task SetDisplayMessage(string ChargePointID, SetDisplayMessageRequest request)
        {
            var msg = _messageFactory.CreateMessage("SetDisplayMessage", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task UnpublishFirmware(string ChargePointID, UnpublishFirmwareRequest request)
        {
            var msg = _messageFactory.CreateMessage("UnpublishFirmware", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task UpdateFirmware(string ChargePointID, UpdateFirmwareRequest request)
        {
            var msg = _messageFactory.CreateMessage("UpdateFirmware", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task Reset(string ChargePointID, ResetRequest request)
        {
            var msg = _messageFactory.CreateMessage("Reset", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task ChangeAvailability(string ChargePointID, ChangeAvailabilityRequest request)
        {
            var msg = _messageFactory.CreateMessage("ChangeAvailability", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task TriggerMessage(string ChargePointID, TriggerMessageRequest request)
        {
            var msg = _messageFactory.CreateMessage("TriggerMessage", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }

        public async Task RefreshConnectors(string ChargePointID)
        {
            Random random = new Random();
            GetBaseReportRequest request = new GetBaseReportRequest
            {
                RequestId = random.Next(100000, 10000000),
                ReportBase = ReportBaseEnumType.SummaryInventory,
                CustomData = new CustomDataType { VendorId = ControllerOCPP20.VendorId }
            };
            
            var msg = _messageFactory.CreateMessage("GetBaseReport", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
        }
    }
}