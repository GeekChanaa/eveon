import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GetChargingProfileStatusEnum } from "./_enums/GetChargingProfileStatusEnum";

  
  export interface GetChargingProfilesResponse {
    customData?: CustomDataType;
    status: GetChargingProfileStatusEnum;
    statusInfo?: StatusInfoType;
  }
  