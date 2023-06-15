import { CustomDataType } from "./CustomDataType";
import { ChargingProfilePurposeEnumType } from "./_enums/ChargingProfilePurposeEnumType";

export interface ClearChargingProfileType {
    customData?: CustomDataType;
    evseId?: number;
    chargingProfilePurpose?: ChargingProfilePurposeEnumType;
    stackLevel?: number;
  }