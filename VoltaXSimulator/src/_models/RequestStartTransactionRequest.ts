import { ChargingProfileType } from "./ChargingProfileType";
import { IdTokenType } from "./IdTokenType";
import { CustomDataType } from "./CustomDataType";

  /**
   * OCPP 2.0.1 FINAL
   */
  export interface RequestStartTransactionRequest {
    customData?: CustomDataType;
    evseId?: number;
    groupIdToken?: IdTokenType;
    idToken: IdTokenType;
    remoteStartId: number;
    chargingProfile?: ChargingProfileType;
  }
  