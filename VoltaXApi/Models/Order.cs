namespace VoltaXApi.Models
{
    public class Order : IEntity
    {
        public int ID { get; set; }
        public int CardID { get; set; }
        public double Amount { get; set; }
        public DateTime RechargeDate { get; set; }
        public Card? Card { get; set; }
    }
}