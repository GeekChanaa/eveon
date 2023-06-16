import { CustomDataType } from "./CustomDataType";
import { EVSEType } from "./EVSEType";
import { MessageTriggerEnumType } from "./_enums/MessageTriggerEnumType";

export interface TriggerMessageRequest {
    customData?: CustomDataType;
    evse?: EVSEType;
    requestedMessage: MessageTriggerEnumType;
  }

  
  

  