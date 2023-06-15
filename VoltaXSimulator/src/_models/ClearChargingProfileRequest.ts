import { ClearChargingProfileType } from "./ClearChargingProfileType";
import { CustomDataType } from "./CustomDataType";


  
  export interface ClearChargingProfileRequest {
    customData?: CustomDataType;
    chargingProfileId?: number;
    chargingProfileCriteria?: ClearChargingProfileType;
  }
  