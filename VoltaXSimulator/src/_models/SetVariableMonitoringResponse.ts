import { CustomDataType } from "./CustomDataType";
import { SetMonitoringResultType } from "./SetMonitoringResultType";

  
  
  
 export interface SetVariableMonitoringResponse {
    customData?: CustomDataType;
    setMonitoringResult: SetMonitoringResultType[];
  }
  