import { CustomDataType } from "./CustomDataType";
import { SalesTariffEntryType } from "./SalesTariffEntryType";

export interface SalesTariffType {
    customData?: CustomDataType;
    id: number;
    salesTariffDescription?: string;
    numEPriceLevels?: number;
    salesTariffEntry: SalesTariffEntryType[];
  }
  