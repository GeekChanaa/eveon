import { ChargingProfileType } from "./ChargingProfileType";
import { CustomDataType } from "./CustomDataType";
import { IdTokenType } from "./IdTokenType";

  
  export interface RequestStartTransactionRequest {
    customData?: CustomDataType;
    evseId?: number;
    groupIdToken?: IdTokenType;
    idToken: IdTokenType;
    remoteStartId: number;
    chargingProfile?: ChargingProfileType;
  }
  
  
  