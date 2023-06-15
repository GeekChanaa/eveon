import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { LogStatusEnumType } from "./_enums/LogStatusEnumType";


  
  export interface GetLogResponse {
    customData?: CustomDataType;
    status: LogStatusEnumType;
    statusInfo?: StatusInfoType;
    filename?: string;
  }
  