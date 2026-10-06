namespace VoltaXApi.OCPP.Ocpp16.SmartCharging
{
    /// <summary>OCPP 1.6 ClearChargingProfile.req: by id, or every profile matching the given fields (none = all).</summary>
    public class ClearChargingProfileRequest
    {
        public int? Id { get; set; }

        public int? ConnectorId { get; set; }

        public ChargingProfilePurposeType? ChargingProfilePurpose { get; set; }

        public int? StackLevel { get; set; }
    }

    public enum ClearChargingProfileStatus
    {
        Accepted,
        Unknown
    }

    public class ClearChargingProfileResponse
    {
        public ClearChargingProfileStatus Status { get; set; }
    }
}
