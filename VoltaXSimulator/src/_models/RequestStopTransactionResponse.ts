import { StatusInfoType } from "./StatusInfoType";
import { CustomDataType } from "./CustomDataType";

  
  /**
   * OCPP 2.0.1 FINAL
   */
  export interface RequestStopTransactionResponse {
    customData?: CustomDataType;
    status: "Accepted" | "Rejected";
    statusInfo?: StatusInfoType;
  }
  