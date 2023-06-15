import { CustomDataType } from "./CustomDataType";

export  interface ChargingSchedulePeriodType {
    customData: CustomDataType;
    startPeriod: number;
    limit: number;
    numberPhases?: number;
    phaseToUse?: number;
  }