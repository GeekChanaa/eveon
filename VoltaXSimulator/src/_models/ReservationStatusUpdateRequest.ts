import { ReservationUpdateStatusEnumType } from "./_enums/ReservationUpdateStatusEnumType";
import { CustomDataType } from "./CustomDataType";

  /**
   * OCPP 2.0.1 FINAL
   */
  export interface ReservationStatusUpdateRequest {
    customData?: CustomDataType;
    reservationId: number;
    reservationUpdateStatus: ReservationUpdateStatusEnumType;
  }
  