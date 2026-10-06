using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Ocpp16;

namespace VoltaXApi.OCPP.Services
{
  public interface IConfigurationService
  {
    Task<SetNetworkProfileResponse> SetNetworkProfile(string chargePointID, SetNetworkProfileRequest request, CancellationToken cancellationToken = default);
    Task<ClearDisplayMessageResponse> ClearDisplayMessage(string chargePointID, ClearDisplayMessageRequest request, CancellationToken cancellationToken = default);
    Task<GetDisplayMessagesResponse> GetDisplayMessages(string chargePointID, GetDisplayMessagesRequest request, CancellationToken cancellationToken = default);
    Task<PublishFirmwareResponse> PublishFirmware(string chargePointID, PublishFirmwareRequest request, CancellationToken cancellationToken = default);
    Task<SetDisplayMessageResponse> SetDisplayMessage(string chargePointID, SetDisplayMessageRequest request, CancellationToken cancellationToken = default);
    Task<UnpublishFirmwareResponse> UnpublishFirmware(string chargePointID, UnpublishFirmwareRequest request, CancellationToken cancellationToken = default);
    Task<UpdateFirmwareResponse> UpdateFirmware(string chargePointID, UpdateFirmwareRequest request, CancellationToken cancellationToken = default);
    Task<ResetResponse> Reset(string chargePointID, ResetRequest request, CancellationToken cancellationToken = default);
    Task<ChangeAvailabilityResponse> ChangeAvailability(string chargePointID, ChangeAvailabilityRequest request, CancellationToken cancellationToken = default);
    Task<TriggerMessageResponse> TriggerMessage(string chargePointID, TriggerMessageRequest request, CancellationToken cancellationToken = default);
    Task<GetBaseReportResponse> RefreshConnectors(string chargePointID, CancellationToken cancellationToken = default);

    // OCPP 1.6 only (OcppProtocolNotSupportedException for other versions).
    Task<GetDiagnostics16Response> GetDiagnostics(string chargePointID, GetDiagnostics16Request request, CancellationToken cancellationToken = default);
    Task<StatusResponse16> ChangeConfiguration(string chargePointID, ChangeConfigurationDto request, CancellationToken cancellationToken = default);
    Task<GetConfiguration16Response> GetConfiguration(string chargePointID, GetConfigurationDto request, CancellationToken cancellationToken = default);
  }
}
