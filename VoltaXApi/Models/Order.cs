namespace VoltaXApi.Models
{
    public class Order : IEntity
    {
        public int ID { get; set; }
        public int? CardID { get; set; }
        public double Amount { get; set; }
        public RechargeOrderStatus Status { get; set; }
        public DateTime RechargeDate { get; set; }
        public Card? Card { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}