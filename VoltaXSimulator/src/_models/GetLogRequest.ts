import { CustomDataType } from "./CustomDataType";
import { LogParametersType } from "./LogParametersType";

  
  
  export interface GetLogRequest {
    customData?: CustomDataType;
    log: LogParametersType;
    logType: "DiagnosticsLog" | "SecurityLog";
    requestId: number;
    retries?: number;
    retryInterval?: number;
  }
  