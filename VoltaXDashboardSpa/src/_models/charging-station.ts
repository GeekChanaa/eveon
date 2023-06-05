import { ChargePoint } from "./charge-point";

export interface ChargingStation {
    id: number;
    name: string; // name of the station
    businessHours: string; // business hours of the station
    address: string; // location of the station
    chargePoints: ChargePoint[]; // Array of car chargers
    [key: string]: any;
}