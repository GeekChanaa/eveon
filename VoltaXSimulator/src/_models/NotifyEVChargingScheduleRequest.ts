import { ChargingScheduleType } from "./ChargingScheduleType";
import { CustomDataType } from "./CustomDataType";




  
  export interface NotifyEVChargingScheduleRequest {
    customData?: CustomDataType;
    timeBase: string;
    chargingSchedule: ChargingScheduleType;
    evseId: number;
  }
  