import { ChargePointCategoryEnum } from "./_enums/charge-point-category";
import { ChargePointStatusEnum } from "./_enums/charge-point-status";
import { Connector } from "./connector";
import { Transaction } from "./transaction";

export interface ChargePoint {
    id: number;
    chargePointId: string;
    chargingStationID : number;
    name: string;
    serialNumber: string;
    make: string;
    category: ChargePointCategoryEnum;
    status: ChargePointStatusEnum;
    comment: string;
    username: string;
    password: string;
    clientCertThumb: string;
    connectors : Connector[];
    transactions: Transaction[];
    [key: string]: any;
  }
  