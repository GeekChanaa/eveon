import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ChargingProfileStatusEnumType } from "./_enums/ChargingProfileStatusEnumType";

export interface SetChargingProfileResponse {
    customData?: CustomDataType;
    status: ChargingProfileStatusEnumType;
    statusInfo?: StatusInfoType;
}
