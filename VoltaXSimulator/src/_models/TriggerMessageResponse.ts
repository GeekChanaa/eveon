import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { TriggerMessageStatusEnumType } from "./_enums/TriggerMessageStatusEnumType";

export interface TriggerMessageResponse {
    customData?: CustomDataType;
    status: TriggerMessageStatusEnumType;
    statusInfo?: StatusInfoType;
  }

  