import { ConsumptionCostType } from "./ConsumptionCostType";
import { CustomDataType } from "./CustomDataType";
import { RelativeTimeIntervalType } from "./RelativeTimeIntervalType";

export interface SalesTariffEntryType {
    customData?: CustomDataType;
    relativeTimeInterval: RelativeTimeIntervalType;
    ePriceLevel: number;
    consumptionCost: ConsumptionCostType[];
  }
  