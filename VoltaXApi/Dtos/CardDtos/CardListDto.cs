using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CardListDto 
    {
        public int ID { get; set; }
        public string CardNumber { get; set; }
        public CardTypeEnum CardType { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int MaxCount { get; set; }
        public CardStatusEnum Status { get; set; }
        public double Balance { get; set; }
        public string Note { get; set; }
        public int? UserID { get; set; }
        public string UserName {get; set;}
    }
}