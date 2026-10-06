using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Display message commands whose results land in DisplayMessageSnapshots: every successful set / clear is followed by a
    /// GetDisplayMessages, whose NotifyDisplayMessages answer becomes the charger's latest snapshot.
    /// </summary>
    public interface IDisplayMessageService
    {
        Task<GetDisplayMessagesResponse> RefreshAsync(string chargePointID, CancellationToken cancellationToken = default);
        Task<SetDisplayMessageResponse> SetAsync(string chargePointID, SetDisplayMessageRequest request, CancellationToken cancellationToken = default);
        Task<ClearDisplayMessageResponse> ClearAsync(string chargePointID, ClearDisplayMessageRequest request, CancellationToken cancellationToken = default);
    }

    public class DisplayMessageService : IDisplayMessageService
    {
        private readonly IConfigurationService _configuration;
        private readonly IOcppDeviceDataService _deviceData;
        private readonly ILogger<DisplayMessageService> _logger;

        public DisplayMessageService(IConfigurationService configuration, IOcppDeviceDataService deviceData, ILogger<DisplayMessageService> logger)
        {
            _configuration = configuration;
            _deviceData = deviceData;
            _logger = logger;
        }

        public async Task<GetDisplayMessagesResponse> RefreshAsync(string chargePointID, CancellationToken cancellationToken = default)
        {
            var requestId = Random.Shared.Next(1, int.MaxValue);
            var response = await _configuration.GetDisplayMessages(chargePointID, new GetDisplayMessagesRequest { RequestId = requestId }, cancellationToken);
            // Unknown: the charger has no message, and sends no NotifyDisplayMessages.
            if (response.Status == GetDisplayMessagesStatusEnumType.Unknown)
                await _deviceData.RecordDisplayMessagesAsync(chargePointID, requestId, Array.Empty<MessageInfoType>(), cancellationToken);
            return response;
        }

        public async Task<SetDisplayMessageResponse> SetAsync(string chargePointID, SetDisplayMessageRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _configuration.SetDisplayMessage(chargePointID, request, cancellationToken);
            if (response.Status == DisplayMessageStatusEnumType.Accepted) await TryRefresh(chargePointID, cancellationToken);
            return response;
        }

        public async Task<ClearDisplayMessageResponse> ClearAsync(string chargePointID, ClearDisplayMessageRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _configuration.ClearDisplayMessage(chargePointID, request, cancellationToken);
            if (response.Status == ClearMessageStatusEnum.Accepted) await TryRefresh(chargePointID, cancellationToken);
            return response;
        }

        private async Task TryRefresh(string chargePointID, CancellationToken cancellationToken)
        {
            try
            {
                await RefreshAsync(chargePointID, cancellationToken);
            }
            catch (Exception ex) when (ex is TimeoutException or Exceptions.OcppCallErrorException or Exceptions.WebSocketNotFoundException)
            {
                // The command itself succeeded; the snapshot is refreshed on the next GetDisplayMessages.
                _logger.LogWarning(ex, "Display messages of {ChargePointId} could not be refreshed", chargePointID);
            }
        }
    }
}
