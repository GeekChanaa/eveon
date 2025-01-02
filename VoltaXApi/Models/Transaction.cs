using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class Transaction  : IEntity
    {
        public int ID { get; set; }
        public string? Uid { get; set; }
        public int ChargingSessionID { get; set; }
        public int? ConnectorID { get; set; }
        public int? StartCardID { get; set; }
        public DateTime StartTime { get; set; }
        public double MeterStart { get; set; }
        public string? StartResult { get; set; }
        public int? StopCardID { get; set; }
        public DateTime? StopTime { get; set; }
        public double? MeterStop { get; set; }
        public string? StopReason { get; set; }
        public double Amount { get; set; }
        public Card? StartCard { get; set; }
        public Card? StopCard { get; set; }
        public ChargingSession? ChargingSession { get; set; }
        public Connector? Connector { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}