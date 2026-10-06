namespace VoltaXApi.Models
{
    /// <summary>
    /// A charging profile sent to (or reported by) a charge point. <see cref="Status"/> is the charger's real answer;
    /// <see cref="LastError"/> keeps why a send got none (Timeout, NotConnected) or the CALLERROR code.
    /// CSMS-created profiles use their own ID as <see cref="OcppProfileId"/>, which keeps the OCPP id unique per charger.
    /// </summary>
    public class ChargingProfile : IEntity
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        /// <summary>0 = the whole charger (1.6 connectorId 0).</summary>
        public int EvseId { get; set; }
        public int OcppProfileId { get; set; }
        public int StackLevel { get; set; }
        public ChargingProfilePurposeEnum Purpose { get; set; }
        public ChargingProfileKindEnum Kind { get; set; }
        public ChargingProfileRecurrencyEnum? RecurrencyKind { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        /// <summary>OCPP transaction id of a TxProfile (2.0.1 string, 1.6 integer as text).</summary>
        public string? TransactionId { get; set; }
        public ChargingRateUnitEnum ChargingRateUnit { get; set; }
        public DateTime? StartSchedule { get; set; }
        public int? Duration { get; set; }
        public double? MinChargingRate { get; set; }
        /// <summary>JSON array of { startPeriod, limit, numberPhases, phaseToUse }.</summary>
        public string PeriodsJson { get; set; } = "[]";
        public ChargingProfileSourceEnum Source { get; set; }
        public ChargingProfileStatusEnum Status { get; set; }
        public string? LastError { get; set; }
        /// <summary>Origin of a ChargingStationExternalConstraints limit reported by the charger (EMS, SO, Other...).</summary>
        public string? ChargingLimitSource { get; set; }
        public int? ChargingStrategyID { get; set; }
        public DateTime? LastSentAt { get; set; }
        public int? CreatedByUserID { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
