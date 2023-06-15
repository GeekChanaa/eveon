import { ChargePoint } from "./charge-point";

export interface Connector {
    id: number;
    chargePointId: number;
    connectorType?: string;
    power: number;
    speed: number;
    chargePoint?: ChargePoint;
    [key: string]: any;
  }
  