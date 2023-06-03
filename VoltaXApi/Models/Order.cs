namespace VoltaXApi.Models
{
    public class Order
    {
    
        public int ID { get; set; }
    
        public int? UserID { get; set; }
    
        public int CardID { get; set; }
    
        public int CarChargerID { get; set; }
    
        public DateTime StartTime { get; set; }
    
        public DateTime StopTime { get; set; }
    
        public int Duration { get; set; }
    
        public string StopReason { get; set; }

        public User User { get; set; }
        public Card Card { get; set; }
        public CarCharger CarCharger { get; set; }
        
    
    }
}