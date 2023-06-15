namespace VoltaXApi.Models
{
    public class Administrator : IEntity
    {
    
        public int ID { get; set; }
    
        public int UserID { get; set; }
        public User User { get; set; }
    
    }
}