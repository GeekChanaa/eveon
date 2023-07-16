

namespace VoltaXApi.Models
{
    public class NotificationSetting : IEntity
    {
        public int ID { get; set; }
        
        public bool Email { get; set; }
        
        public User? User { get; set; }
        
        public int UserID { get; set; }
        
        public int NotificationTypeID { get; set; }
        
        public NotificationType? NotificationType {get; set;}
        
        public bool Urgent { get; set; }
        public bool Active { get; set; }
        
    }
}