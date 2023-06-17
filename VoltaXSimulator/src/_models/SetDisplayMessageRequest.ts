import { CustomDataType } from "./CustomDataType";
import { MessageInfoType } from "./MessageInfoType";

export interface SetDisplayMessageRequest {
    customData?: CustomDataType;
    message: MessageInfoType;
}
