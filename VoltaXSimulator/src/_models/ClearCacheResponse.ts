import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ClearCacheStatusEnumType } from "./_enums/ClearCacheStatusEnumType";

  
 export interface ClearCacheResponse {
    customData?: CustomDataType;
    status: ClearCacheStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  