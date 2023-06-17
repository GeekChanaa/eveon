import { ChargingProfileType } from "./ChargingProfileType";
import { CustomDataType } from "./CustomDataType";

  export interface SetChargingProfileRequest {
    customData?: CustomDataType;
    evseId: number;
    chargingProfile: ChargingProfileType;
  }
  