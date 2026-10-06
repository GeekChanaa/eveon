using VoltaXApi.OCPP.Core;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Ocpp16;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>Configuration and firmware commands. Every command waits for the charger's answer (see <see cref="IOcppCommandSender"/>).</summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IOcppCommandSender _commandSender;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly Ocpp16CommandService _ocpp16;

        public ConfigurationService(IOcppCommandSender commandSender, ILogger<ConfigurationService> logger, Ocpp16CommandService ocpp16)
        {
            _commandSender = commandSender;
            _ocpp16 = ocpp16;
            _logger = logger;
        }

        public async Task<SetNetworkProfileResponse> SetNetworkProfile(string chargePointID, SetNetworkProfileRequest request, CancellationToken cancellationToken = default)
        {
            _ocpp16.Require201(chargePointID, "SetNetworkProfile");
            var response = await _commandSender.SendRequestAsync<SetNetworkProfileRequest, SetNetworkProfileResponse>(chargePointID, "SetNetworkProfile", request, cancellationToken: cancellationToken);
            _logger.LogInformation("SetNetworkProfile answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<ClearDisplayMessageResponse> ClearDisplayMessage(string chargePointID, ClearDisplayMessageRequest request, CancellationToken cancellationToken = default)
        {
            _ocpp16.Require201(chargePointID, "ClearDisplayMessage");
            var response = await _commandSender.SendRequestAsync<ClearDisplayMessageRequest, ClearDisplayMessageResponse>(chargePointID, "ClearDisplayMessage", request, cancellationToken: cancellationToken);
            _logger.LogInformation("ClearDisplayMessage answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<GetDisplayMessagesResponse> GetDisplayMessages(string chargePointID, GetDisplayMessagesRequest request, CancellationToken cancellationToken = default)
        {
            _ocpp16.Require201(chargePointID, "GetDisplayMessages");
            var response = await _commandSender.SendRequestAsync<GetDisplayMessagesRequest, GetDisplayMessagesResponse>(chargePointID, "GetDisplayMessages", request, cancellationToken: cancellationToken);
            _logger.LogInformation("GetDisplayMessages answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<PublishFirmwareResponse> PublishFirmware(string chargePointID, PublishFirmwareRequest request, CancellationToken cancellationToken = default)
        {
            _ocpp16.Require201(chargePointID, "PublishFirmware");
            var response = await _commandSender.SendRequestAsync<PublishFirmwareRequest, PublishFirmwareResponse>(chargePointID, "PublishFirmware", request, cancellationToken: cancellationToken);
            _logger.LogInformation("PublishFirmware answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<SetDisplayMessageResponse> SetDisplayMessage(string chargePointID, SetDisplayMessageRequest request, CancellationToken cancellationToken = default)
        {
            _ocpp16.Require201(chargePointID, "SetDisplayMessage");
            var response = await _commandSender.SendRequestAsync<SetDisplayMessageRequest, SetDisplayMessageResponse>(chargePointID, "SetDisplayMessage", request, cancellationToken: cancellationToken);
            _logger.LogInformation("SetDisplayMessage answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<UnpublishFirmwareResponse> UnpublishFirmware(string chargePointID, UnpublishFirmwareRequest request, CancellationToken cancellationToken = default)
        {
            _ocpp16.Require201(chargePointID, "UnpublishFirmware");
            var response = await _commandSender.SendRequestAsync<UnpublishFirmwareRequest, UnpublishFirmwareResponse>(chargePointID, "UnpublishFirmware", request, cancellationToken: cancellationToken);
            _logger.LogInformation("UnpublishFirmware answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<UpdateFirmwareResponse> UpdateFirmware(string chargePointID, UpdateFirmwareRequest request, CancellationToken cancellationToken = default)
        {
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.UpdateFirmware(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<UpdateFirmwareRequest, UpdateFirmwareResponse>(chargePointID, "UpdateFirmware", request, cancellationToken: cancellationToken);
            _logger.LogInformation("UpdateFirmware answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<ResetResponse> Reset(string chargePointID, ResetRequest request, CancellationToken cancellationToken = default)
        {
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.Reset(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<ResetRequest, ResetResponse>(chargePointID, "Reset", request, cancellationToken: cancellationToken);
            _logger.LogInformation("Reset answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<ChangeAvailabilityResponse> ChangeAvailability(string chargePointID, ChangeAvailabilityRequest request, CancellationToken cancellationToken = default)
        {
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.ChangeAvailability(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<ChangeAvailabilityRequest, ChangeAvailabilityResponse>(chargePointID, "ChangeAvailability", request, cancellationToken: cancellationToken);
            _logger.LogInformation("ChangeAvailability answered by {ChargePointId}", chargePointID);
            return response;
        }

        public async Task<TriggerMessageResponse> TriggerMessage(string chargePointID, TriggerMessageRequest request, CancellationToken cancellationToken = default)
        {
            var response = _ocpp16.IsOcpp16(chargePointID)
                ? await _ocpp16.TriggerMessage(chargePointID, request, cancellationToken)
                : await _commandSender.SendRequestAsync<TriggerMessageRequest, TriggerMessageResponse>(chargePointID, "TriggerMessage", request, cancellationToken: cancellationToken);
            _logger.LogInformation("TriggerMessage answered by {ChargePointId}", chargePointID);
            return response;
        }

        /// <summary>Asks for a SummaryInventory base report; the connectors arrive later with NotifyReport.</summary>
        public async Task<GetBaseReportResponse> RefreshConnectors(string chargePointID, CancellationToken cancellationToken = default)
        {
            if (_ocpp16.IsOcpp16(chargePointID))
                return await _ocpp16.RefreshConnectors(chargePointID, cancellationToken);

            var request = new GetBaseReportRequest
            {
                RequestId = Random.Shared.Next(100000, 10000000),
                ReportBase = ReportBaseEnumType.SummaryInventory
            };
            var response = await _commandSender.SendRequestAsync<GetBaseReportRequest, GetBaseReportResponse>(chargePointID, "GetBaseReport", request, cancellationToken: cancellationToken);
            _logger.LogInformation("RefreshConnectors (GetBaseReport {RequestId}) answered by {ChargePointId}: {Status}", request.RequestId, chargePointID, response.Status);
            return response;
        }

        public Task<GetDiagnostics16Response> GetDiagnostics(string chargePointID, GetDiagnostics16Request request, CancellationToken cancellationToken = default) =>
            _ocpp16.GetDiagnostics(chargePointID, request, cancellationToken);

        public Task<StatusResponse16> ChangeConfiguration(string chargePointID, ChangeConfigurationDto request, CancellationToken cancellationToken = default) =>
            _ocpp16.ChangeConfiguration(chargePointID, request, cancellationToken);

        public Task<GetConfiguration16Response> GetConfiguration(string chargePointID, GetConfigurationDto request, CancellationToken cancellationToken = default) =>
            _ocpp16.GetConfiguration(chargePointID, request, cancellationToken);
    }
}
