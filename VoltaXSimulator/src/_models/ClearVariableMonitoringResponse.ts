import { ClearMonitoringResultType } from "./ClearMonitoringResultType";
import { CustomDataType } from "./CustomDataType";


  
 
  
  export interface ClearVariableMonitoringResponse {
    customData?: CustomDataType;
    clearMonitoringResult: ClearMonitoringResultType[];
  }
  