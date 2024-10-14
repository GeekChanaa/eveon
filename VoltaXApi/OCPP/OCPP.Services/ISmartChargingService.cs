

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Services
{
  public interface ISmartChargingService
  {
    Task ClearChargingProfile(string chargePointID, ClearChargingProfileRequest request);
    Task GetChargingProfiles(string chargePointID, GetChargingProfilesRequest request);
    Task SetChargingProfile(string chargePointID, SetChargingProfileRequest request);
    Task ClearedChargingLimit(string chargePointID, ClearedChargingLimitRequest request);
    Task GetCompositeSchedule(string chargePointID, GetCompositeScheduleRequest request);
  }
}