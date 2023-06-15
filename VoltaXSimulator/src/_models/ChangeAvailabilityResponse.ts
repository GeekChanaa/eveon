import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ChangeAvailabilityStatusEnumType } from "./_enums/ChangeAvailabilityStatusEnumType";

  export interface ChangeAvailabilityResponse {
    customData?: CustomDataType;
    status: ChangeAvailabilityStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  