namespace VoltaXApi.Models
{
    /// <summary>Load balancing settings of a charging station (one row per station).</summary>
    public class StationLoadLimit : IEntity
    {
        public int ID { get; set; }
        public int ChargingStationID { get; set; }
        public ChargingStation? ChargingStation { get; set; }
        public bool Enabled { get; set; }
        /// <summary>Per phase. When both limits are set the lower one applies.</summary>
        public double? MaxCurrentA { get; set; }
        public double? MaxPowerKW { get; set; }
        public int Phases { get; set; } = 3;
        public double Voltage { get; set; } = 230;
        public double MinPerSessionA { get; set; } = 6;
        public LoadBalancingStrategyEnum Strategy { get; set; } = LoadBalancingStrategyEnum.EqualShare;
        public double SafetyMarginPercent { get; set; }
        public DateTime? LastRebalancedAt { get; set; }
        public int? UpdatedByUserID { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
