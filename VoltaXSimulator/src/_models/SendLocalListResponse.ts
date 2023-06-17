import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { SendLocalListStatusEnumType } from "./_enums/SendLocalListStatusEnumType";

  /**
   * OCPP 2.0.1 FINAL
   */
  export interface SendLocalListResponse {
    customData?: CustomDataType;
    status: SendLocalListStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  