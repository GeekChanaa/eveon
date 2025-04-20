import { Card } from "./card";
import { ChargePoint } from "./charge-point";

export interface Transaction {
    id: number;
    uid?: string;
    chargePointID?: string;
    connectorID: number;
    startTagId: string;
    startTime: Date;
    meterStart: number;
    startResult: string;
    stopTagId: string;
    stopTime: Date;
    meterStop: number;
    stopReason: string;
    status : string;
    amount: number;
    cardID: number;
    [key: string]: any;
    card?: Card;
    chargePoint?: ChargePoint;
}
