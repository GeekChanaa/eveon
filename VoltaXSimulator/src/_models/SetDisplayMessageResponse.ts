import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { DisplayMessageStatusEnumType } from "./_enums/DisplayMessageStatusEnumType";

  export interface SetDisplayMessageResponse {
    customData?: CustomDataType;
    status: DisplayMessageStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  