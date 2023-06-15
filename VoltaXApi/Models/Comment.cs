namespace VoltaXApi.Models
{
    public class Comment : IEntity
    {
    
        public int ID { get; set; }
    
        public int UserID { get; set; }
    
        public int Rating { get; set; }
    
        public string Text { get; set; }
    
        public int? ChargingStationID { get; set; }
    
        public int? ChargePointID { get; set; }
    
        public DateTime CommentTime { get; set; }
        public User? User { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public ChargingStation? ChargingStation { get; set; }
        
        
        
        
    
    }
}