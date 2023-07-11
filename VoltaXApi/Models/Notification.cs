using System;

namespace VoltaXApi.Models
{
    public class Notification : IEntity
    {
        public int ID { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public bool Deleted { get; set; } = false;
        
        public NotificationType NotificationType { get; set; }
        
        public int NotificationTypeID { get; set; }
        
        public bool Read { get; set; }
        
        public string Url { get; set; }
        
        public User Sender { get; set; }
        
        public int? SenderID { get; set; }

        public int? ReceiverID { get; set; }
        
        public User Receiver { get; set; }
        
        
        public string Action { get; set; }

        public string ActionOn { get; set; }
        
        public string Description { get; set; }
        public bool Urgent { get; set; }
        
        
    }
}