import { ChargingStationType } from "./ChargingStationType";
import { CustomDataType } from "./CustomDataType";
import { BootReasonEnumType } from "./_enums/BootReasonEnumType";

export interface BootNotificationRequest {
    customData?: CustomDataType;
    chargingStation: ChargingStationType;
    reason: BootReasonEnumType;
  }