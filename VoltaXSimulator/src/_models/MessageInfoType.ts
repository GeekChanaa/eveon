import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { MessageContentType } from "./MessageContentType";
import { MessagePriorityEnumType } from "./_enums/MessagePriorityEnumType";
import { MessageStateEnumType } from "./_enums/MessageStateEnumType";

export interface MessageInfoType {
    customData?: CustomDataType;
    display?: ComponentType;
    id: number;
    priority: MessagePriorityEnumType;
    state?: MessageStateEnumType;
    startDateTime?: string;
    endDateTime?: string;
    transactionId?: string;
    message: MessageContentType;
  }