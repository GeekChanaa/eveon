import { ChargePoint } from "./charge-point";

export interface ChargingStation {
    id: number;
    name: string;
    address: string;
    network: string;
    category: string;
    chargerQuantity: string;
    country: string;
    state: string;
    city: string;
    latitude: string;
    longitude: string;
    organisation: string;
    parkingType: string;
    status: string;
    wifiAmenity: string;
    parkingAmenity: string;
    restaurantsAmenity: string;
    washroomAmenity: string;
    sittingAreaAmenity: string;
    chargePoints: ChargePoint[];
    [key: string]: any;
  }
  