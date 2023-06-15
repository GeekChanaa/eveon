import { CustomDataType } from "./CustomDataType";
import { UploadLogStatusEnum } from "./_enums/UploadLogStatusEnum";

  
  export interface LogStatusNotificationRequest {
    customData?: CustomDataType;
    status: UploadLogStatusEnum;
    requestId?: number;
  }
  