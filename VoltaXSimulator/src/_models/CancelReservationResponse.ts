import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { CancelReservationStatusEnumType } from "./_enums/CancelReservationStatusEnumType";

  
  export interface CancelReservationResponse {
    customData?: CustomDataType;
    status: CancelReservationStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  