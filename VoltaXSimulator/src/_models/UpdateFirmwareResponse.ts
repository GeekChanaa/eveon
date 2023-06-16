import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
  
  export interface UpdateFirmwareResponse {
    customData?: CustomDataType;
    status: UpdateFirmwareStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  
  export type UpdateFirmwareStatusEnumType =
    | "Accepted"
    | "Rejected"
    | "AcceptedCanceled"
    | "InvalidCertificate"
    | "RevokedCertificate";
  

  