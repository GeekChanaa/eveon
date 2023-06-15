import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ClearMessageStatusEnumType } from "./_enums/ClearMessageStatusEnumType";


  
  export interface ClearDisplayMessageResponse {
    customData?: CustomDataType;
    status: ClearMessageStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  