
namespace VoltaXApi.Dtos
{
    public class ConnectorCreateDto
    {
        public string ConnectorID { get; set; }
        public string EvseID { get; set; }
        public string? ConnectorType { get; set; }
        public double Power { get; set; } = 0 ;
        public double PricePerKWh { get; set; }   
        public double FlatFee { get; set; }  = 0 ;     
        public double PricePerMinute { get; set; }   
        public double PricePerIdleMinute { get; set; }   
        public double PricePerHour { get; set; }  
        public double CostPerKwh { get; set; }  
        public double MaxPower { get; set; }  
        public TimeSpan? StartTime { get; set; }    
        public TimeSpan? EndTime { get; set; }      
    
    }
}