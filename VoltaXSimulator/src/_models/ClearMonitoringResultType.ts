import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ClearMonitoringStatusEnumType } from "./_enums/ClearMonitoringStatusEnumType";

export  interface ClearMonitoringResultType {
    customData?: CustomDataType;
    status: ClearMonitoringStatusEnumType;
    id: number;
    statusInfo?: StatusInfoType;
  }