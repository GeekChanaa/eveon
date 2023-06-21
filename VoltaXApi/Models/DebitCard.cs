namespace VoltaXApi.Models
{
    public class DebitCard : IEntity
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public string CardNumber { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string CVV { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}