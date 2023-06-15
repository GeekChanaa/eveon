import { CustomDataType } from "./CustomDataType";

export interface SignedMeterValueType {
    customData?: CustomDataType;
    signedMeterData: string;
    signingMethod: string;
    encodingMethod: string;
    publicKey: string;
  }