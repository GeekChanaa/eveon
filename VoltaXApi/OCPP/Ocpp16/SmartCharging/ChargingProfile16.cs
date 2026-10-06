using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Ocpp16.SmartCharging
{
    // OCPP 1.6 smart charging types, serialized with OCPPMessageFactory.DefaultSettings (camelCase, enum names, no nulls).

    public enum ChargingProfilePurposeType
    {
        ChargePointMaxProfile,
        TxDefaultProfile,
        TxProfile
    }

    public enum ChargingProfileKindType
    {
        Absolute,
        Recurring,
        Relative
    }

    public enum RecurrencyKindType
    {
        Daily,
        Weekly
    }

    public enum ChargingRateUnitType
    {
        W,
        A
    }

    public class ChargingSchedulePeriod
    {
        [Required]
        public int StartPeriod { get; set; }

        /// <summary>Multiple of 0.1 (1.6 schema).</summary>
        [Required]
        public decimal Limit { get; set; }

        public int? NumberPhases { get; set; }
    }

    public class ChargingSchedule
    {
        public int? Duration { get; set; }

        public DateTime? StartSchedule { get; set; }

        [Required]
        public ChargingRateUnitType ChargingRateUnit { get; set; }

        [Required]
        [MinLength(1)]
        public List<ChargingSchedulePeriod> ChargingSchedulePeriod { get; set; } = new();

        public decimal? MinChargingRate { get; set; }
    }

    public class CsChargingProfiles
    {
        [Required]
        public int ChargingProfileId { get; set; }

        public int? TransactionId { get; set; }

        [Required]
        public int StackLevel { get; set; }

        [Required]
        public ChargingProfilePurposeType ChargingProfilePurpose { get; set; }

        [Required]
        public ChargingProfileKindType ChargingProfileKind { get; set; }

        public RecurrencyKindType? RecurrencyKind { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        [Required]
        public ChargingSchedule ChargingSchedule { get; set; }
    }
}
