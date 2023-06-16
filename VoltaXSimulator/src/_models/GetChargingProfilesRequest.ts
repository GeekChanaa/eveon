import { ChargingProfileCriterionType } from "./ChargingProfileCriterionType";
import { CustomDataType } from "./CustomDataType";

  
  export interface GetChargingProfilesRequest {
    customData?: CustomDataType;
    requestId: number;
    evseId?: number;
    chargingProfile: ChargingProfileCriterionType;
  }
  