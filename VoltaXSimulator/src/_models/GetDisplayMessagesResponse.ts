import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";

  export interface GetDisplayMessagesResponse {
    customData?: CustomDataType;
    status: GetDisplayMessagesStatusEnum;
    statusInfo?: StatusInfoType;
  }
  