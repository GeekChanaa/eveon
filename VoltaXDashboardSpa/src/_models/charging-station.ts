import { ChargingStationCategoryEnum } from "./_enums/charging-station-category";
import { ChargingStationStatusEnum } from "./_enums/charging-station-status";
import { ParkingTypeEnum } from "./_enums/parking-type";
import { ChargePoint } from "./charge-point";

export interface ChargingStation {
    id: number;
    name: string;
    address: string;
    network: string;
    category: ChargingStationCategoryEnum;
    chargerQuantity: string;
    country: string;
    state: string;
    city: string;
    latitude: string;
    longitude: string;
    organisation: string;
    parkingType: ParkingTypeEnum;
    status: ChargingStationStatusEnum;
    wifiAmenity: string;
    parkingAmenity: string;
    restaurantsAmenity: string;
    washroomAmenity: string;
    sittingAreaAmenity: string;
    chargePoints: ChargePoint[];
    [key: string]: any;
  }
  