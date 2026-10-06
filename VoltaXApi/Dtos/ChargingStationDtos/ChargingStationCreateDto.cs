using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ChargingStationCreateDto
    {
        [Required, StringLength(300)]
        public string Address { get; set; } 
        
        [JsonConverter(typeof(StringEnumConverter))]
        public ChargingStationNetworkEnum Network { get; set; }
        public ChargingStationCategoryEnum Category { get; set; }
        [Range(0, 100)]
        public int ChargerQuantity { get; set; }
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(100)]
        public string? State { get; set; }
        
        [Required, StringLength(100)]
        public string City { get; set; }
        
        [RegularExpression(@"^-?\d{1,2}(\.\d+)?$", ErrorMessage = "Latitude must be a decimal number.")]
        public string? Latitude { get; set; }
        
        [RegularExpression(@"^-?\d{1,3}(\.\d+)?$", ErrorMessage = "Longitude must be a decimal number.")]
        public string? Longitude { get; set; }
        
        [StringLength(200)]
        public string? Organisation { get; set; }
        
        public ParkingTypeEnum ParkingType { get; set; }
        
        public ChargingStationStatusEnum Status { get; set; }
        
        public bool WifiAmenity { get; set; }
        public bool ParkingAmenity { get; set; }
        public bool RestaurantsAmenity { get; set; }
        public bool WashroomAmenity { get; set; }
        public bool SittingAreaAmenity { get; set; }
        public int? PartnerID { get; set; }
        [MaxLength(50)]
        public ICollection<ChargePointCreateDto>? ChargePoints { get; set; }
        public IEnumerable<IFormFile>? ChargingStationImages { get; set; }
    }
}