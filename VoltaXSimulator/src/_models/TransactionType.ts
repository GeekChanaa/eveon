import { CustomDataType } from "./CustomDataType";
import { ChargingStateEnumType } from "./_enums/ChargingStateEnumType";
import { ReasonEnumType } from "./_enums/ReasonEnumType";

  
  export interface TransactionType {
    customData: CustomDataType;
    transactionId: string;
    chargingState?: ChargingStateEnumType;
    timeSpentCharging?: number;
    stoppedReason?: ReasonEnumType;
    remoteStartId?: number;
  }
  