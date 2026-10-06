using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.SmartCharging
{
    /// <summary>A charging profile to send, independent of the charger's OCPP version.</summary>
    public class ChargingProfileInputDto
    {
        /// <summary>Stored profile to replace (same OCPP id on the charger); null creates a new profile.</summary>
        public int? ChargingProfileID { get; set; }

        [Range(0, int.MaxValue)]
        public int EvseId { get; set; }

        [Range(0, int.MaxValue)]
        public int StackLevel { get; set; }

        public ChargingProfilePurposeEnum Purpose { get; set; } = ChargingProfilePurposeEnum.TxDefaultProfile;
        public ChargingProfileKindEnum Kind { get; set; } = ChargingProfileKindEnum.Absolute;
        public ChargingProfileRecurrencyEnum? RecurrencyKind { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        /// <summary>Absolute: defaults to now; Recurring: defaults to today's business-local midnight.</summary>
        public DateTime? StartSchedule { get; set; }
        public int? Duration { get; set; }
        public ChargingRateUnitEnum ChargingRateUnit { get; set; } = ChargingRateUnitEnum.A;
        public double? MinChargingRate { get; set; }

        [MaxLength(36)]
        public string? TransactionId { get; set; }

        [Required]
        public List<ChargingProfilePeriod> Periods { get; set; } = new();
    }

    public class ChargingProfileDto
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public int EvseId { get; set; }
        public int OcppProfileId { get; set; }
        public int StackLevel { get; set; }
        public ChargingProfilePurposeEnum Purpose { get; set; }
        public ChargingProfileKindEnum Kind { get; set; }
        public ChargingProfileRecurrencyEnum? RecurrencyKind { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? TransactionId { get; set; }
        public ChargingRateUnitEnum ChargingRateUnit { get; set; }
        public DateTime? StartSchedule { get; set; }
        public int? Duration { get; set; }
        public double? MinChargingRate { get; set; }
        public List<ChargingProfilePeriod> Periods { get; set; } = new();
        public ChargingProfileSourceEnum Source { get; set; }
        public ChargingProfileStatusEnum Status { get; set; }
        public string? LastError { get; set; }
        public string? ChargingLimitSource { get; set; }
        public int? ChargingStrategyID { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int? CreatedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static ChargingProfileDto From(ChargingProfile p) => new()
        {
            ID = p.ID,
            ChargePointID = p.ChargePointID,
            EvseId = p.EvseId,
            OcppProfileId = p.OcppProfileId,
            StackLevel = p.StackLevel,
            Purpose = p.Purpose,
            Kind = p.Kind,
            RecurrencyKind = p.RecurrencyKind,
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
            TransactionId = p.TransactionId,
            ChargingRateUnit = p.ChargingRateUnit,
            StartSchedule = p.StartSchedule,
            Duration = p.Duration,
            MinChargingRate = p.MinChargingRate,
            Periods = SmartChargingJson.Deserialize<ChargingProfilePeriod>(p.PeriodsJson),
            Source = p.Source,
            Status = p.Status,
            LastError = p.LastError,
            ChargingLimitSource = p.ChargingLimitSource,
            ChargingStrategyID = p.ChargingStrategyID,
            LastSentAt = p.LastSentAt,
            CreatedByUserID = p.CreatedByUserID,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }

    public class ClearStoredChargingProfileDto
    {
        [Required]
        public int ChargingProfileID { get; set; }
    }

    public class CompositeScheduleRequestDto
    {
        [Range(0, int.MaxValue)]
        public int EvseId { get; set; }

        /// <summary>Seconds.</summary>
        [Range(1, 7 * 86_400)]
        public int Duration { get; set; } = 86_400;

        public ChargingRateUnitEnum? ChargingRateUnit { get; set; }
    }

    /// <summary>The charger's real answer (<see cref="Status"/>) to SetChargingProfile, and the stored profile.</summary>
    public sealed record ChargingProfileCommandResult(string Status, string? Reason, ChargingProfileDto Profile);

    public sealed record ClearChargingProfileResult(string Status, int ClearedCount);

    public sealed record GetChargingProfilesResult(string Status, int RequestId);

    /// <summary>GetCompositeSchedule answer of a 2.0.1 or 1.6 charger, in one shape.</summary>
    public sealed record CompositeScheduleResult(
        string Status,
        int EvseId,
        DateTime? ScheduleStart,
        int? Duration,
        ChargingRateUnitEnum? ChargingRateUnit,
        List<ChargingProfilePeriod> Periods,
        string? Reason);
}
