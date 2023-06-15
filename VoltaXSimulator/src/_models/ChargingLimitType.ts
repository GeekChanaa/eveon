import { CustomDataType } from "./CustomDataType";
import { ChargingLimitSourceEnumType } from "./_enums/ChargingLimitSourceEnumType";

export interface ChargingLimitType {
    customData?: CustomDataType;
    chargingLimitSource: ChargingLimitSourceEnumType;
    isGridCritical?: boolean;
  }