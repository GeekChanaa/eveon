using Microsoft.EntityFrameworkCore;

namespace VoltaXApi.Models
{
    public class Order : IEntity
    {
        public int ID { get; set; }
        [NotUpdatable]
        public int CardID { get; set; }
        [NotUpdatable]
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        [NotUpdatable]
        public RechargeOrderStatus Status { get; set; }
        public DateTime RechargeDate { get; set; }
        public Card? Card { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}