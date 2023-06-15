import { CustomDataType } from "./CustomDataType";

  
  export interface GetTransactionStatusRequest {
    customData?: CustomDataType;
    transactionId: string;
  }
  