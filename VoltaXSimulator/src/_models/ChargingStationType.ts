import { CustomDataType } from "./CustomDataType";
import { ModemType } from "./Modemtype";

export interface ChargingStationType {
    customData?: CustomDataType;
    serialNumber?: string;
    model: string;
    modem?: ModemType;
    vendorName: string;
    firmwareVersion?: string;
  }
  