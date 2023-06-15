import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GenericDeviceModelStatusEnum } from "./_enums/GenericDeviceModelStatusEnum";

  export interface GetBaseReportResponse {
    customData?: CustomDataType;
    status: GenericDeviceModelStatusEnum;
    statusInfo?: StatusInfoType;
  }
  