import { CustomDataType } from "./CustomDataType";

export interface DCChargingParametersType {
    customData?: CustomDataType;
    evMaxCurrent: number;
    evMaxVoltage: number;
    energyAmount?: number;
    evMaxPower?: number;
    stateOfCharge?: number;
    evEnergyCapacity?: number;
    fullSoC?: number;
    bulkSoC?: number;
  }