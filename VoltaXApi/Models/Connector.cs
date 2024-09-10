namespace VoltaXApi.Models
{
    public class Connector  : IEntity
    {
    
        public int ID { get; set; }
        public string? ConnectorID{ get; set; }
        public int? ChargePointID { get; set; }
        public string? ConnectorType { get; set; }
        public decimal Power { get; set; } = 0 ;
        public double? Speed { get; set; }
        public decimal PricePerKWh { get; set; }   
        public decimal FlatFee { get; set; }  = 0 ;     
        public decimal PricePerMinute { get; set; }   
        public decimal PricePerHour { get; set; }  
        public decimal MaxPower { get; set; }  
        public TimeSpan? StartTime { get; set; }    
        public TimeSpan? EndTime { get; set; }      
        public ChargePoint? ChargePoint { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}