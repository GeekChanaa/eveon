

namespace VoltaXApi.Dtos
{
    public class ConnectorListDto
    {
        public int ID { get; set; }
        public string? ConnectorType { get; set; }
        public int? ConnectorID { get; set; }
        public int? EvseID { get; set; }
        public double Power { get; set; }
        public double PricePerKWh { get; set; }   
        public double PricePerMinute { get; set; }   
        public double PricePerIdleMinute { get; set; }   
        public double PricePerHour { get; set; }  
        public double FlatFee { get; set; }  
        public double CostPerKwh { get; set; }  
    }
}