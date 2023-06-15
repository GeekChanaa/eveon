import { ChargingLimitType } from "./ChargingLimitType";
import { ChargingScheduleType } from "./ChargingScheduleType";
import { CustomDataType } from "./CustomDataType";


  
  export interface NotifyChargingLimitRequest {
    customData?: CustomDataType;
    chargingSchedule: ChargingScheduleType[];
    evseId?: number;
    chargingLimit: ChargingLimitType;
  }
  