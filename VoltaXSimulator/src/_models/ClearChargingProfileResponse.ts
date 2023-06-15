import { CustomDataType } from "./CustomDataType";
import { ClearChargingProfileStatusEnumType } from "./_enums/ClearChargingProfileStatusEnumType";
import { StatusInfoType } from "./StatusInfoType";

 
  
  export interface ClearChargingProfileResponse {
    customData?: CustomDataType;
    status: ClearChargingProfileStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  