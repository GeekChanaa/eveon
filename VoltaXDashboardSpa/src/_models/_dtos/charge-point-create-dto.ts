import { ChargePointCategoryEnum } from "../_enums/charge-point-category";
import { ChargePointStatusEnum } from "../_enums/charge-point-status";


export interface ChargePointCreateDto {
    chargePointId: string;
    name: string;
    serialNumber: string;
    make: string;
    status: ChargePointStatusEnum;
    comment: string;
    username: string;
    password: string;
    category : ChargePointCategoryEnum;
    clientCertThumb: string;
    connectors : any[];
  }
  