using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ChargingStationDisplayDto
    {
        public int ID { get; set; }
        public string Name { get; set; } 
        public string Address { get; set; } 
        public ChargingStationNetworkEnum Network { get; set; }
        public ChargingStationCategoryEnum Category { get; set; }
        public int ChargerQuantity { get; set; }
        public string Country { get; set; }
        
        public string State { get; set; }
        
        public string City { get; set; }
        
        public string Latitude { get; set; }
        
        public string Longitude { get; set; }
        
        public string Organisation { get; set; }
        
        public ParkingTypeEnum ParkingType { get; set; }
        
        public ChargingStationStatusEnum Status { get; set; }
        public int? PartnerID { get; set; }
        public string? PartnerName { get; set; }
        
        public bool WifiAmenity { get; set; }
        public bool ParkingAmenity { get; set; }
        public bool RestaurantsAmenity { get; set; }
        public bool WashroomAmenity { get; set; }
        public bool SittingAreaAmenity { get; set; }
    }
}