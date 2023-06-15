import { CustomDataType } from "./CustomDataType";

  export interface GetTransactionStatusResponse {
    customData?: CustomDataType;
    ongoingIndicator: boolean;
    messagesInQueue: boolean;
  }
  