import { StatusInfoType } from "./StatusInfoType";
import { RequestStartStopStatusEnumType } from "./_enums/RequestStartStopStatusEnumType";
import { CustomDataType } from "./CustomDataType";

  /**
   * OCPP 2.0.1 FINAL
   */
  export interface RequestStartTransactionResponse {
    customData?: CustomDataType;
    status: RequestStartStopStatusEnumType;
    statusInfo?: StatusInfoType;
    transactionId?: string;
  }
  