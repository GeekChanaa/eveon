import { CustomDataType } from "./CustomDataType";
import { MonitoringBaseEnumType } from "./_enums/MonitoringBaseEnumType";

  export interface SetMonitoringBaseRequest {
    customData?: CustomDataType;
    monitoringBase: MonitoringBaseEnumType;
  }
  