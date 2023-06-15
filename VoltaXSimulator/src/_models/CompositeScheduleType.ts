import { ChargingSchedulePeriodType } from "./ChargingSchedulePeriodType";
import { CustomDataType } from "./CustomDataType";
import { ChargingRateUnitEnum } from "./_enums/ChargingRateUnitEnum";

  
 export  interface CompositeScheduleType {
    customData: CustomDataType;
    chargingSchedulePeriod: ChargingSchedulePeriodType[];
    evseId: number;
    duration: number;
    scheduleStart: string;
    chargingRateUnit: ChargingRateUnitEnum;
  }
  