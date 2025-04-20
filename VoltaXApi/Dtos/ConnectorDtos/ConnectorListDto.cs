

namespace VoltaXApi.Dtos
{
    public class ConnectorListDto
    {
        public int ID { get; set; }
        public string? ConnectorType { get; set; }
        public int? ConnectorID { get; set; }
        public int? EvseID { get; set; }
        public decimal Power { get; set; }
        public decimal PricePerKWh { get; set; }   
        public decimal PricePerMinute { get; set; }   
        public decimal PricePerIdleMinute { get; set; }   
        public decimal PricePerHour { get; set; }  
        public decimal FlatFee { get; set; }  
        public decimal CostPerKwh { get; set; }  
    }
}