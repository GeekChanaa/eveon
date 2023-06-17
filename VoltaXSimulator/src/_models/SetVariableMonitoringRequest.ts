import { CustomDataType } from "./CustomDataType";
import { SetMonitoringDataType } from "./SetMonitoringDataType";

  export interface SetVariableMonitoringRequest {
    customData?: CustomDataType;
    setMonitoringData: SetMonitoringDataType[];
  }
  