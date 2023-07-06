using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class ChargePoint : IEntity
    {
        [Key]
        public int ID { get; set; }
        
        
        public string ChargePointId { get; set; }
        public int ChargingStationID { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public string Make { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientCertThumb { get; set; }
        public string Category { get; set; }

        public ChargingStation? ChargingStation { get; set; }

        public virtual ICollection<Connector>? Connectors { get; set; }
        public virtual ICollection<Transaction>? Transactions { get; set; }
        
        
    
    }
}