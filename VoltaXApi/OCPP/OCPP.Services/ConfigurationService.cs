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
        private readonly ILogger<ConfigurationService> _logger;
        
        public ConfigurationService(
            OCPPMessageProcessor messageProcessor,
            ILogger<ConfigurationService> logger)
        {
            _messageProcessor = messageProcessor;
            _messageFactory = new OCPPMessageFactory();
            _logger = logger;
        }

        public async Task SetNetworkProfile(string ChargePointID, SetNetworkProfileRequest request)
        {
            _logger.LogInformation("Starting SetNetworkProfile for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("SetNetworkProfile", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed SetNetworkProfile for ChargePointID: {ChargePointID}", ChargePointID);
        }
        
        public async Task ClearDisplayMessage(string ChargePointID, ClearDisplayMessageRequest request)
        {
            _logger.LogInformation("Starting ClearDisplayMessage for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("ClearDisplayMessage", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed ClearDisplayMessage for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task GetDisplayMessages(string ChargePointID, GetDisplayMessagesRequest request)
        {
            _logger.LogInformation("Starting GetDisplayMessages for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("GetDisplayMessages", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed GetDisplayMessages for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task PublishFirmware(string ChargePointID, PublishFirmwareRequest request)
        {
            _logger.LogInformation("Starting PublishFirmware for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("PublishFirmware", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed PublishFirmware for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task SetDisplayMessage(string ChargePointID, SetDisplayMessageRequest request)
        {
            _logger.LogInformation("Starting SetDisplayMessage for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("SetDisplayMessage", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed SetDisplayMessage for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task UnpublishFirmware(string ChargePointID, UnpublishFirmwareRequest request)
        {
            _logger.LogInformation("Starting UnpublishFirmware for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("UnpublishFirmware", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed UnpublishFirmware for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task UpdateFirmware(string ChargePointID, UpdateFirmwareRequest request)
        {
            _logger.LogInformation("Starting UpdateFirmware for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("UpdateFirmware", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed UpdateFirmware for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task Reset(string ChargePointID, ResetRequest request)
        {
            _logger.LogInformation("Starting Reset for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("Reset", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed Reset for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task ChangeAvailability(string ChargePointID, ChangeAvailabilityRequest request)
        {
            _logger.LogInformation("Starting ChangeAvailability for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("ChangeAvailability", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed ChangeAvailability for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task TriggerMessage(string ChargePointID, TriggerMessageRequest request)
        {
            _logger.LogInformation("Starting TriggerMessage for ChargePointID: {ChargePointID}", ChargePointID);
            var msg = _messageFactory.CreateMessage("TriggerMessage", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed TriggerMessage for ChargePointID: {ChargePointID}", ChargePointID);
        }

        public async Task RefreshConnectors(string ChargePointID)
        {
            _logger.LogInformation("Starting RefreshConnectors for ChargePointID: {ChargePointID}", ChargePointID);
            Random random = new Random();
            GetBaseReportRequest request = new GetBaseReportRequest
            {
                RequestId = random.Next(100000, 10000000),
                ReportBase = ReportBaseEnumType.SummaryInventory,
                // CustomData = new CustomDataType { VendorId = ControllerOCPP20.VendorId }
            };
            
            var msg = _messageFactory.CreateMessage("GetBaseReport", request);
            await _messageProcessor.SendMessage(msg, ChargePointID);
            _logger.LogInformation("Completed RefreshConnectors for ChargePointID: {ChargePointID}", ChargePointID);
        }
    }
}
