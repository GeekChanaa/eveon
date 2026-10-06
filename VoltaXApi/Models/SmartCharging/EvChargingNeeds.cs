namespace VoltaXApi.Models
{
    /// <summary>NotifyEVChargingNeeds (ISO 15118) of an EVSE, plus the EV's own schedule from NotifyEVChargingSchedule.</summary>
    public class EvChargingNeeds : IEntity
    {
        public int ID { get; set; }
        public int ChargePointID { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public int EvseId { get; set; }
        public string? RequestedEnergyTransfer { get; set; }
        public DateTime? DepartureTime { get; set; }
        public int? MaxScheduleTuples { get; set; }
        /// <summary>Wh.</summary>
        public double? EnergyAmount { get; set; }
        public double? EvMinCurrent { get; set; }
        public double? EvMaxCurrent { get; set; }
        public double? EvMaxVoltage { get; set; }
        /// <summary>W (DC).</summary>
        public double? EvMaxPower { get; set; }
        public int? StateOfCharge { get; set; }
        public double? EvEnergyCapacity { get; set; }
        public int? FullSoC { get; set; }
        public int? BulkSoC { get; set; }
        public DateTime? EvScheduleTimeBase { get; set; }
        public string? EvScheduleJson { get; set; }
        public DateTime ReceivedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
