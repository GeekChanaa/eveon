import { CustomDataType } from "./CustomDataType";
import { IdTokenInfoType } from "./IdTokenInfoType";
import { MessageContentType } from "./MessageContentType";

  
  export interface TransactionEventResponseType {
    customData?: CustomDataType;
    totalCost?: number;
    chargingPriority?: number;
    idTokenInfo?: IdTokenInfoType;
    updatedPersonalMessage?: MessageContentType;
  }
  