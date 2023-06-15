import { CustomDataType } from "./CustomDataType";

export interface ACChargingParametersType {
    customData?: CustomDataType;
    energyAmount: number;
    evMinCurrent: number;
    evMaxCurrent: number;
    evMaxVoltage: number;
  }