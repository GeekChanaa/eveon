import { CostType } from "./CostType";
import { CustomDataType } from "./CustomDataType";

export interface ConsumptionCostType {
    customData?: CustomDataType;
    startValue: number;
    cost: CostType[];
  }
  