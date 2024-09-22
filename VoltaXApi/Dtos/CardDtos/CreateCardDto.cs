using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CreateCardDto
    {
        public int ID { get; set; }
        public string? CardNumber { get; set; }
        public CardTypeEnum CardType { get; set; }
        public DateTime ExpirationDate { get; set; } = new DateTime().AddYears(2);
        public int MaxCount { get; set; } = 3000;
        public CardStatusEnum Status { get; set; }
        public double Balance { get; set; }
        public string Note { get; set; }
        public int? UserID { get; set; }
    }
}