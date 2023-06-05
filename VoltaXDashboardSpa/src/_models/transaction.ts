import { ChargePoint } from "./charge-point";
import { User } from "./user";

export interface Transaction {
    id: number;
    userID: number;
    chargePointID: number;
    startTime: Date;
    endTime: Date;
    energyConsumed: number;
    paymentAmount: number;
    transactionStatus: string;
    user : User | null;
    chargePoint : ChargePoint | null;
    [key: string]: any;
}
