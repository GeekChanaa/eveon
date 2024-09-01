export interface ChargingStationCreateDto {
    Address: string;
    Network: string;
    Category: string;
    ChargerQuantity: string;
    City: string;
    ParkingType: string;
    Status: string;
    WifiAmenity: string;
    ParkingAmenity: string;
    RestaurantsAmenity: string;
    WashroomAmenity: string;
    SittingAreaAmenity: string;
    chargePoints : any[]
}
  