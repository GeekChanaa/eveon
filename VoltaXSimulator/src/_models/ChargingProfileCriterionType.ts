import { CustomDataType } from "./CustomDataType";
import { ChargingLimitSourceEnum } from "./_enums/ChargingLimitSourceEnum";
import { ChargingProfilePurposeEnum } from "./_enums/ChargingProfilePurposeEnum";

export interface ChargingProfileCriterionType {
    customData: CustomDataType;
    chargingProfilePurpose: ChargingProfilePurposeEnum;
    stackLevel?: number;
    chargingProfileId?: number[];
    chargingLimitSource?: ChargingLimitSourceEnum[];
  }