import { CustomDataType } from "./CustomDataType";
import { CostKindEnumType } from "./_enums/CostKindEnumType";

export interface CostType {
    customData?: CustomDataType;
    costKind: CostKindEnumType;
    amount: number;
    amountMultiplier?: number;
  }
  