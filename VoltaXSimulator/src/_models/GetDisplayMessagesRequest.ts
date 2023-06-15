import { CustomDataType } from "./CustomDataType";
import { MessagePriorityEnum } from "./_enums/MessagePriorityEnum";
import { MessageStateEnum } from "./_enums/MessageStateEnum";

  export interface GetDisplayMessagesRequest {
    customData?: CustomDataType;
    id?: number[];
    requestId: number;
    priority?: MessagePriorityEnum;
    state?: MessageStateEnum;
  }
  