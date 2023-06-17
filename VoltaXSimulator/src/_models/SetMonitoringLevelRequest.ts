import { CustomDataType } from "./CustomDataType";

  export interface SetMonitoringLevelRequest {
    customData?: CustomDataType;
    severity: number;
  }
  