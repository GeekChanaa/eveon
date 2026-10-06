using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos
{
    /// <summary>POST api/ChargePoint. Identity, QR value and the OCPP password are server-managed.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public class ChargePointCreateRequestDto
    {
        [Required, Range(1, int.MaxValue)]
        public int ChargingStationID { get; set; }

        [Required, StringLength(100, MinimumLength = 1)]
        public string SerialNumber { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int? ChargePointModelID { get; set; }

        [Range(1, int.MaxValue)]
        public int? ChargePointBrandID { get; set; }

        [EnumDataType(typeof(ChargePointStatusEnum))]
        public ChargePointStatusEnum Status { get; set; }

        [EnumDataType(typeof(ChargePointCategoryEnum))]
        public ChargePointCategoryEnum Category { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        [StringLength(64)]
        [RegularExpression(@"^[\x21-\x39\x3B-\x7E]*$", ErrorMessage = "Username must be printable ASCII without spaces or ':'.")]
        public string? Username { get; set; }

        [StringLength(128)]
        [RegularExpression(@"^[A-Fa-f0-9: ]*$", ErrorMessage = "Certificate thumbprint must be hexadecimal.")]
        public string? ClientCertThumb { get; set; }

        public bool? ShowOnMap { get; set; }
        public bool? HasChargeCable { get; set; }

        [MaxLength(2)]
        public List<ChargePointConnectorCreateDto>? Connectors { get; set; }
    }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public class ChargePointConnectorCreateDto
    {
        [Range(0, 100)]
        public int? ConnectorID { get; set; }

        [Range(0, 100)]
        public int EvseID { get; set; }

        public ConnectorEnumType? ConnectorType { get; set; }
        /// <summary>Alias the dashboard form sends.</summary>
        public ConnectorEnumType? Type { get; set; }

        [Range(0, 1000)] public double? Power { get; set; }
        [Range(0, 1000)] public double? MaxPower { get; set; }
        [Range(0, 10000)] public double? PricePerKWh { get; set; }
        [Range(0, 10000)] public double? PricePerMinute { get; set; }
        [Range(0, 10000)] public double? PricePerIdleMinute { get; set; }
        [Range(0, 10000)] public double? CostPerKwh { get; set; }
        [Range(0, 10000)] public double? FlatFee { get; set; }
    }

    /// <summary>PUT api/ChargePoint/{id}: only the fields present (non-null) are changed.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public class ChargePointUpdateRequestDto
    {
        [StringLength(100, MinimumLength = 1)]
        public string? SerialNumber { get; set; }

        [Range(1, int.MaxValue)]
        public int? ChargePointModelID { get; set; }

        [Range(1, int.MaxValue)]
        public int? ChargePointBrandID { get; set; }

        [EnumDataType(typeof(ChargePointStatusEnum))]
        public ChargePointStatusEnum? Status { get; set; }

        [EnumDataType(typeof(ChargePointCategoryEnum))]
        public ChargePointCategoryEnum? Category { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        [StringLength(128)]
        [RegularExpression(@"^[A-Fa-f0-9: ]*$", ErrorMessage = "Certificate thumbprint must be hexadecimal.")]
        public string? ClientCertThumb { get; set; }

        public bool? ShowOnMap { get; set; }
        public bool? HasChargeCable { get; set; }
    }
}
