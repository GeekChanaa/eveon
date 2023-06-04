namespace VoltaXApi.Models
{
    public class Administrator
    {
    
        public int ID { get; set; }
    
        public int UserID { get; set; }
        public User User { get; set; }
    
    }
}