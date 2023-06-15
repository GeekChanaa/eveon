import { ChargingSchedulePeriodType } from "./ChargingSchedulePeriodType";
import { CustomDataType } from "./CustomDataType";
import { SalesTariffType } from "./SalesTariffType";
import { ChargingRateUnitEnumType } from "./_enums/ChargingRateUnitEnumType";

  
  export interface ChargingScheduleType {
    customData?: CustomDataType;
    id?: number;
    startSchedule?: string;
    duration?: number;
    chargingRateUnit: ChargingRateUnitEnumType;
    chargingSchedulePeriod: ChargingSchedulePeriodType[];
    minChargingRate?: number;
    salesTariff?: SalesTariffType;
  }