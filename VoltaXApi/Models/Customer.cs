namespace VoltaXApi.Models
{
    public class Customer : IEntity
    {
    
        public int ID { get; set; }
    
        public int UserID { get; set; }
    
        public Boolean Sold { get; set; }
        public User? User { get; set; }
    }
}