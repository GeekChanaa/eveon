import { Connector } from "./connector";

export interface ChargePointApi {
    id: number;
    chargePointId: string;
    chargingStationID : number;
    name: string;
    serialNumber: string;
    make: string;
    status: string;
    comment: string;
    username: string;
    password: string;
    clientCertThumb: string;
    [key: string]: any;
  }
  