import { ChargingStation } from "./charging-station";

export interface ChargingStationImage {
    id: number;
    chargingStationID : number;
    imageID : number;
    chargingStation : ChargingStation;
}