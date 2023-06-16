import { UnlockStatusEnumType } from "./_enums/UnlockStatusEnumType";
import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";

export interface UnlockConnectorResponse {
    customData?: CustomDataType;
    status: UnlockStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  

  
  