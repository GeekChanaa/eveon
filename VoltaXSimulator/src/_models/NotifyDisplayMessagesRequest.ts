import { CustomDataType } from "./CustomDataType";
import { MessageInfoType } from "./MessageInfoType";

export interface NotifyDisplayMessagesRequest {
    customData?: CustomDataType;
    messageInfo: MessageInfoType[];
    requestId: number;
    tbc?: boolean;
  }
  
  
  

  