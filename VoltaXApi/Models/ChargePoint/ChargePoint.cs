using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class ChargePoint  : IEntity
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        [Required]
        public int? ChargingStationID { get; set; }
        public string? SerialNumber { get; set; }
        public string? VendorName { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Model { get; set; }
        public string? Make { get; set; } = "VoltaX";
        public int? ChargePointModelID { get; set;}
        public ChargePointStatusEnum Status { get; set; }
        public string? Comment { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool? ShowOnMap { get; set; } = true;
        public bool? HasChargeCable { get; set; } = true;
        public string? ClientCertThumb { get; set; } = "";
        public ChargePointCategoryEnum Category { get; set; }
        public ChargingStation? ChargingStation { get; set; }
        public virtual ICollection<Connector>? Connectors { get; set; }
        public virtual ICollection<Transaction>? Transactions { get; set; }
        public virtual ChargePointModel? ChargePointModel {get;set;}
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}