namespace VoltaXApi.Models
{
    public class ChargePoint
    {
    
        public int ID { get; set; }
    
        public int ChargingStationID { get; set; }
    
        public string Network { get; set; }
    
        public string Timezone { get; set; }
    
        public DateTime LastConnectTime { get; set; }
    
        public DateTime OnlineTime { get; set; }
        public ChargingStation ChargingStation { get; set; }
        
        
    
    }
}