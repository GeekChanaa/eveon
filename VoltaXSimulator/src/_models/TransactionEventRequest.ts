import { CustomDataType } from "./CustomDataType";
import { EVSEType } from "./EVSEType";
import { IdTokenType } from "./IdTokenType";
import { MeterValueType } from "./MeterValueType";
import { TransactionType } from "./TransactionType";
import { TransactionEventEnumType } from "./_enums/TransactionEventEnumType";
import { TriggerReasonEnumType } from "./_enums/TriggerReasonEnumType";

  export interface TransactionEventRequest {
    customData?: CustomDataType;
    eventType: TransactionEventEnumType;
    meterValue: MeterValueType[];
    timestamp: string;
    triggerReason: TriggerReasonEnumType;
    seqNo: number;
    offline?: boolean;
    numberOfPhasesUsed?: number;
    cableMaxCurrent?: number;
    reservationId?: number;
    transactionInfo: TransactionType;
    evse: EVSEType;
    idToken: IdTokenType;
  }
  