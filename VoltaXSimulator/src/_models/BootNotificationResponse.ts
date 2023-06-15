import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { RegistrationStatusEnumType } from "./_enums/RegistrationStatusEnumType";
  
export interface BootNotificationResponse {
    customData?: CustomDataType;
    currentTime: string;
    interval: number;
    status: RegistrationStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  