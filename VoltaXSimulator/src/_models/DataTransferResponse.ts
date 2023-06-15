import { CustomDataType } from "./CustomDataType";
import { DataTransferStatusEnumType } from "./_enums/DataTransferStatusEnumType";
import { StatusInfoType } from "./StatusInfoType";

  
  export interface DataTransferResponse {
    customData?: CustomDataType;
    status: DataTransferStatusEnumType;
    statusInfo?: StatusInfoType;
    data?: string;
  }
  