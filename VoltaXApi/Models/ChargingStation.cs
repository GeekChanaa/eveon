namespace VoltaXApi.Models
{
    public class ChargingStation
    {
    
        public int ID { get; set; }
    
        public int CarChargerID { get; set; }
    
        public string StationName { get; set; }
    
        public string BusinessHours { get; set; }
    
        public string Address { get; set; }
        public CarCharger CarCharger { get; set; }
        
        
    
    }
}