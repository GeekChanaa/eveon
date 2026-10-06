using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Ocpp16.SmartCharging
{
    /// <summary>OCPP 1.6 GetCompositeSchedule.req; connectorId 0 asks for the expected consumption of the whole charge point.</summary>
    public class GetCompositeScheduleRequest
    {
        [Required]
        public int ConnectorId { get; set; }

        /// <summary>Seconds.</summary>
        [Required]
        public int Duration { get; set; }

        public ChargingRateUnitType? ChargingRateUnit { get; set; }
    }

    public enum GetCompositeScheduleStatus
    {
        Accepted,
        Rejected
    }

    public class GetCompositeScheduleResponse
    {
        public GetCompositeScheduleStatus Status { get; set; }

        public int? ConnectorId { get; set; }

        public DateTime? ScheduleStart { get; set; }

        public ChargingSchedule? ChargingSchedule { get; set; }
    }
}
