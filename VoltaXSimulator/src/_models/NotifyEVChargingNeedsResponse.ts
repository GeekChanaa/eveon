import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { NotifyEVChargingNeedsStatusEnumType } from "./_enums/NotifyEVChargingNeedsStatusEnumType";

export interface NotifyEVChargingNeedsResponse {
    customData?: CustomDataType;
    status: NotifyEVChargingNeedsStatusEnumType;
    statusInfo?: StatusInfoType;
  }

  
  