import { CustomDataType } from "./CustomDataType";

  export interface RequestStopTransactionRequest {
    customData?: CustomDataType;
    transactionId: string;
  }
  