using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class ChargePoint : IEntity
    {
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        [Required]
        public int ChargingStationID { get; set; }
        public string? SerialNumber { get; set; }
        public string? VendorName { get; set; }
        public string? FirmwareVersion { get; set; }
        public int? ChargePointModelID { get; set; }
        public int? ChargePointBrandID { get; set; }
        public ChargePointStatusEnum Status { get; set; }
        public string? Comment { get; set; }
        public string? Username { get; set; }
        /// <summary>
        /// Salted PBKDF2 hash of the OCPP Basic auth password (legacy rows may still hold plain text
        /// until the charger next authenticates). Never serialized; set through ChargePoint/SetPassword.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string? Password { get; set; }
        public bool? ShowOnMap { get; set; } = true;
        public bool? HasChargeCable { get; set; } = true;
        public string? ClientCertThumb { get; set; } = "";
        /// <summary>
        /// OCPP security profile the CSMS enforces at the WebSocket handshake: 1 Basic auth (TLS optional,
        /// plain ws only with Ocpp:AllowInsecureProfile1), 2 TLS + Basic auth, 3 TLS + client certificate.
        /// Changed only through ocpp/Security/SetSecurityProfile.
        /// </summary>
        [NotUpdatable]
        public int SecurityProfile { get; set; } = 1;
        public ChargePointCategoryEnum Category { get; set; }
        public ChargingStation? ChargingStation { get; set; }
        public ChargePointModel? ChargePointModel { get; set; }
        public ChargePointBrand? ChargePointBrand { get; set; }
        public virtual ICollection<Connector>? Connectors { get; set; }
        public virtual ICollection<Transaction>? Transactions { get; set; }
        
        /// <summary>
        /// QR Code value - Stores the complete deep link URL for mobile app
        /// Format: https://app.voltax.com/charge/{uniqueCode}
        /// Example: https://app.voltax.com/charge/A7K9M2X5P8Q1W4Y6Z3N0
        /// Can be changed/regenerated at any time
        /// </summary>
        public string? QrValue { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

//
