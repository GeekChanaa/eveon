import { ACChargingParametersType } from "./ACChargingParametersType";
import { CustomDataType } from "./CustomDataType";
import { DCChargingParametersType } from "./DCChargingParametersType";
import { EnergyTransferModeEnumType } from "./_enums/EnergyTransferModeEnumType";

export interface ChargingNeedsType {
    customData?: CustomDataType;
    acChargingParameters?: ACChargingParametersType;
    dcChargingParameters?: DCChargingParametersType;
    requestedEnergyTransfer: EnergyTransferModeEnumType;
    departureTime?: string;
  }