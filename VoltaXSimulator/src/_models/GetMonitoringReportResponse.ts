import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GenericDeviceModelStatusEnumType } from "./_enums/GenericDeviceModelStatusEnumType";

  
  export interface GetMonitoringReportResponse {
    customData?: CustomDataType;
    status: GenericDeviceModelStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  