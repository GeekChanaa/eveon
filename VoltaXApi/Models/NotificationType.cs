using System;

namespace VoltaXApi.Models
{
    public class NotificationType : IEntity
    {
        public int ID { get; set; }
        
        public string Description { get; set; }
        
        public string Name { get; set; }

        public bool ForCustomers { get; set; }
        public bool ForAdmins { get; set; }
        public bool ForPartners { get; set; }
                
        
    }
}