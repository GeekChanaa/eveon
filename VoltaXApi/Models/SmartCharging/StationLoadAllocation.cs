namespace VoltaXApi.Models
{
    /// <summary>An allocation sent to a session by the load balancer; the latest row of a running session is the live one.</summary>
    public class StationLoadAllocation : IEntity
    {
        public int ID { get; set; }
        public int ChargingStationID { get; set; }
        public int TransactionID { get; set; }
        public int ChargePointID { get; set; }
        public int EvseId { get; set; }
        public int? ConnectorId { get; set; }
        public double AllocatedA { get; set; }
        public double AllocatedKW { get; set; }
        public double StationLimitA { get; set; }
        public bool Queued { get; set; }
        public int? ChargingProfileID { get; set; }
        /// <summary>Accepted, Rejected, Timeout, NotConnected, CallError, Invalid or Failed.</summary>
        public string SendStatus { get; set; } = "Pending";
        public string? Reason { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
