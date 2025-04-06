using System.ComponentModel.DataAnnotations.Schema;

namespace VoltaXApi.Models
{
    public class Card  : IEntity
    {
        public int ID { get; set; }
        public string CardNumber { get; set; }
        [NotMapped]
        public string LastFourDigits { get { return CardNumber.Substring(CardNumber.Length - 4);} }
        public CardTypeEnum CardType { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int MaxCount { get; set; }
        public CardStatusEnum Status { get; set; }
        public double Balance { get; set; }
        public string Note { get; set; }
        public bool? Blocked { get; set; }
        public int? UserID { get; set; }
        public User? User { get; set; }
        public ICollection<Order>? Orders { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}