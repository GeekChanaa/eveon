import { ChargingProfileCriterionType } from "./ChargingProfileCriterionType";

  
  export interface GetChargingProfilesRequest {
    customData?: CustomDataType;
    requestId: number;
    evseId?: number;
    chargingProfile: ChargingProfileCriterionType;
  }
  