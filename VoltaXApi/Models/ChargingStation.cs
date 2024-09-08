using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VoltaXApi.Models
{
    public class ChargingStation  : IEntity
    {
        public int ID { get; set; }
        public string Name { get; set; } 
        public string Address { get; set; } 
        
        [JsonConverter(typeof(StringEnumConverter))]
        public ChargingStationNetworkEnum Network { get; set; }
        public ChargingStationCategoryEnum Category { get; set; }
        public int ChargerQuantity { get; set; }
        public string? Country { get; set; }
        
        public string? State { get; set; }
        
        public string City { get; set; }
        
        public string? Latitude { get; set; }
        
        public string? Longitude { get; set; }
        
        public string? Organisation { get; set; }
        
        public ParkingTypeEnum ParkingType { get; set; }
        
        public ChargingStationStatusEnum Status { get; set; }
        
        public bool WifiAmenity { get; set; }
        public bool ParkingAmenity { get; set; }
        public bool RestaurantsAmenity { get; set; }
        public bool WashroomAmenity { get; set; }
        public bool SittingAreaAmenity { get; set; }
        public int? PartnerID { get; set; }
        // Navigation properties
        public ICollection<ChargePoint>? ChargePoints { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}