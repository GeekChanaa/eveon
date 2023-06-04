import { CarCharger } from "./car-charger";

export interface ChargingStation {
    id: number;
    CarChargerID: number;
    StationName: string;
    BusinessHours: string;
    Address: string;
    CarCharger: CarCharger;
}