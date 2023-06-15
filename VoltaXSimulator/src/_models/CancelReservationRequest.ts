import { CustomDataType } from "./CustomDataType";

export  interface CancelReservationRequest {
    customData?: CustomDataType;
    reservationId: number;
  }