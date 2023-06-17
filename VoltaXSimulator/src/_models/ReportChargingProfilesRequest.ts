import { ChargingProfileType } from "./ChargingProfileType";
import { CustomDataType } from "./CustomDataType";
import { ChargingLimitSourceEnumType } from "./_enums/ChargingLimitSourceEnumType";

export interface ReportChargingProfilesRequest {
    customData?: CustomDataType;
    requestId: number;
    chargingLimitSource: ChargingLimitSourceEnumType;
    chargingProfile: ChargingProfileType[];
    tbc?: boolean;
    evseId: number;
  }