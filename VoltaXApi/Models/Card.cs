namespace VoltaXApi.Models
{
    public class Card : IEntity
    {
    
        public int ID { get; set; }
    
        public string CardNumber { get; set; }
    
        public string Account { get; set; }
    
        public string CardType { get; set; }
    
        public DateTime ExpirationDate { get; set; }
    
        public int MaxCount { get; set; }
    
        public string Status { get; set; }
    
        public decimal Balance { get; set; }
    
        public string Note { get; set; }
    
        public int CustomerID { get; set; }
        public Customer? Customer { get; set; }
        
        
    
    }
}