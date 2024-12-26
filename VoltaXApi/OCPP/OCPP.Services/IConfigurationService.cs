

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface IConfigurationService
  {
    Task SetNetworkProfile(string ChargePointID,SetNetworkProfileRequest request);
    Task ClearDisplayMessage(string ChargePointID, ClearDisplayMessageRequest request);
    Task GetDisplayMessages(string ChargePointID, GetDisplayMessagesRequest request);
    Task PublishFirmware(string ChargePointID, PublishFirmwareRequest request);
    Task SetDisplayMessage(string ChargePointID, SetDisplayMessageRequest request);
    Task UnpublishFirmware(string ChargePointID, UnpublishFirmwareRequest request);
    Task UpdateFirmware(string ChargePointID, UpdateFirmwareRequest request);
    Task Reset(string ChargePointID, ResetRequest request);
    Task ChangeAvailability(string ChargePointID, ChangeAvailabilityRequest request);
    Task TriggerMessage(string ChargePointID, TriggerMessageRequest request);
    Task RefreshConnectors(string ChargePointID);
  }
}