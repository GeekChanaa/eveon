using VoltaXApi.Models;


namespace VoltaXApi.Dtos
{
    public class CardDto
    {
        public string CardNumber { get; set; }
        
        public CardTypeEnum CardType { get; set; }
        public string Name { get; set; }
    
        public DateTime ExpirationDate { get; set; }
    
        public int MaxCount { get; set; }
    
        public CardStatusEnum Status { get; set; }
    
        public decimal Balance { get; set; }
    
        public string Note { get; set; }
    
        public int UserID { get; set; }
    }
}