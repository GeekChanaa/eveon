

namespace VoltaXApi.Dtos
{
    public class ConnectorListDto
    {
        public int ID { get; set; }
        public string? ConnectorType { get; set; }
        public decimal Power { get; set; }
        public double Speed { get; set; }
        public decimal PricePerKWh { get; set; }   
        public decimal PricePerMinute { get; set; }   
        public decimal PricePerHour { get; set; }  
    }
}