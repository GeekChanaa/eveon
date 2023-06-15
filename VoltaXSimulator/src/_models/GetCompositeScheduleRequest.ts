import { CustomDataType } from "./CustomDataType";
import { ChargingRateUnitEnum } from "./_enums/ChargingRateUnitEnum";

  
  interface GetCompositeScheduleRequest {
    customData?: CustomDataType;
    duration: number;
    chargingRateUnit?: ChargingRateUnitEnum;
    evseId: number;
  }
  