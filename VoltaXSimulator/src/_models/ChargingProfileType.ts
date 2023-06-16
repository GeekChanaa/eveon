import { ChargingScheduleType } from "./ChargingScheduleType";
import { CustomDataType } from "./CustomDataType";
import { ChargingProfileKindEnumType } from "./_enums/ChargingProfileKindEnumType";
import { ChargingProfilePurposeEnumType } from "./_enums/ChargingProfilePurposeEnumType";
import { RecurrencyKindEnumType } from "./_enums/RecurrencyKindEnumType";

export interface ChargingProfileType {
    customData: CustomDataType;
    id: number;
    stackLevel: number;
    chargingProfilePurpose: ChargingProfilePurposeEnumType;
    chargingProfileKind: ChargingProfileKindEnumType;
    recurrencyKind?: RecurrencyKindEnumType;
    validFrom?: string;
    validTo?: string;
    chargingSchedule: ChargingScheduleType[];
    transactionId?: string;
  }