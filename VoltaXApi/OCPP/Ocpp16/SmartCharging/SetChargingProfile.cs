using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Ocpp16.SmartCharging
{
    /// <summary>OCPP 1.6 SetChargingProfile.req; connectorId 0 targets the whole charge point.</summary>
    public class SetChargingProfileRequest
    {
        [Required]
        public int ConnectorId { get; set; }

        [Required]
        public CsChargingProfiles CsChargingProfiles { get; set; }
    }

    public enum ChargingProfileStatus
    {
        Accepted,
        Rejected,
        NotSupported
    }

    public class SetChargingProfileResponse
    {
        [Required]
        public ChargingProfileStatus Status { get; set; }
    }
}
