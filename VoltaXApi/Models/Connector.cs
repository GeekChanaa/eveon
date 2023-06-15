namespace VoltaXApi.Models
{
    public class Connector : IEntity
    {
    
        public int ID { get; set; }
    
        public int ChargePointID { get; set; }
        public string? ConnectorType { get; set; }
        public decimal Power { get; set; }
        public double Speed { get; set; }
        public ChargePoint? ChargePoint { get; set; }
    
    }
}