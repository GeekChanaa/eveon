namespace VoltaXApi.Dtos
{
    public class ConnectorCreateDto
    {
        public int ConnectorID { get; set; }
        public string? ConnectorType { get; set; }
        public decimal Power { get; set; } = 0 ;
        public string? Speed { get; set; }
        public decimal PricePerKWh { get; set; }   
        public decimal FlatFee { get; set; }  = 0 ;     
        public decimal PricePerMinute { get; set; }   
        public decimal PricePerHour { get; set; }  
        public decimal MaxPower { get; set; }  
        public TimeSpan? StartTime { get; set; }    
        public TimeSpan? EndTime { get; set; }      
    
    }
}