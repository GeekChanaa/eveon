namespace VoltaXApi.Models
{
    public class ChargingStation
    {
        public int ID { get; set; }
        public string Name { get; set; } // name of the station
        public string Address { get; set; } // location of the station
        public string Network { get; set; }
        public string Category { get; set; }
        public string ChargerQuantity { get; set; }
        public string Country { get; set; }
        
        public string State { get; set; }
        
        public string City { get; set; }
        
        public string Latitude { get; set; }
        
        public string Longitude { get; set; }
        
        public string Organisation { get; set; }
        
        public string ParkingType { get; set; }
        
        public string Status { get; set; }
        
        public string WifiAmenity { get; set; }
        public string ParkingAmenity { get; set; }
        public string RestaurantsAmenity { get; set; }
        public string WashroomAmenity { get; set; }
        public string SittingAreaAmenity { get; set; }
        
        
        

        // Navigation properties
        public ICollection<ChargePoint> ChargePoints { get; set; }
    }
}