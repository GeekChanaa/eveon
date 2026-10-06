namespace VoltaXApi.Models
{
    /// <summary>
    /// Reusable charging profile template. <see cref="PeriodsJson"/> holds { startSeconds, limit, numberPhases }:
    /// seconds since business-local midnight (Daily), since business-local Monday 00:00 (Weekly)
    /// or since the moment the strategy is applied (Absolute).
    /// </summary>
    public class ChargingStrategy : IEntity
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ChargingProfilePurposeEnum Purpose { get; set; } = ChargingProfilePurposeEnum.TxDefaultProfile;
        public ChargingProfileKindEnum Kind { get; set; } = ChargingProfileKindEnum.Recurring;
        public ChargingProfileRecurrencyEnum? RecurrencyKind { get; set; }
        public ChargingRateUnitEnum ChargingRateUnit { get; set; } = ChargingRateUnitEnum.A;
        public int StackLevel { get; set; }
        public string PeriodsJson { get; set; } = "[]";
        public bool IsPredefined { get; set; }
        public int? CreatedByUserID { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
