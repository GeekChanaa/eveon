import { CustomDataType } from "./CustomDataType";
import { ChargingLimitSourceEnumType } from "./_enums/ChargingLimitSourceEnumType";

 
  
  export interface ClearedChargingLimitRequest {
    customData?: CustomDataType;
    chargingLimitSource: ChargingLimitSourceEnumType;
    evseId?: number;
  }
  