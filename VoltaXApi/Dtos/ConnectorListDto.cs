

namespace VoltaXApi.Dtos
{
    public class ConnectorListDto
    {
        public int ID { get; set; }
        public string? ConnectorType { get; set; }
        public decimal Power { get; set; }
        public double Speed { get; set; }
    }
}