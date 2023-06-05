namespace VoltaXApi.Models
{
    public class Connector
    {
    
        public int ID { get; set; }
    
        public int ChargePointID { get; set; }
    
        public string ConnectorType { get; set; }
    
        public decimal Power { get; set; }
        public ChargePoint ChargePoint { get; set; }
        
        
    
    }
}