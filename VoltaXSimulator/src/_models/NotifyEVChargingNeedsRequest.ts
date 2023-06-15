import { ChargingNeedsType } from "./ChargingNeedsType";
import { CustomDataType } from "./CustomDataType";

export interface NotifyEVChargingNeedsRequest {
    customData?: CustomDataType;
    maxScheduleTuples?: number;
    chargingNeeds: ChargingNeedsType;
    evseId: number;
  }

  

  

  
 
  

  