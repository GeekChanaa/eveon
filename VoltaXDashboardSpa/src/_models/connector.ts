import { ChargePoint } from "./charge-point";

export interface Connector {
    id: number;
    chargePointID: number;
    connectorID: number;
    evseID: number;
    connectorType?: string;
    power: number;
    speed: number;
    chargePoint?: ChargePoint;
    pricePerKWh : number; 
    flatFee : number; 
    pricePerMinute : number; 
    pricePerHour : number; 
    costPerKwh : number; 
    maxPower : number; 
    startTime : string; 
    endTime : string; 
    [key: string]: any;
  }
  