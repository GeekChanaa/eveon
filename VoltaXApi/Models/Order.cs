namespace VoltaXApi.Models
{
    public class Order
    {
    
        public int ID { get; set; }
    
        public int? UserID { get; set; }
        public int CardID { get; set; }
        public decimal Amount { get; set; }
        public DateTime RechargeDate { get; set; }
        public User User { get; set; }
        public Card Card { get; set; }
        
    
    }
}