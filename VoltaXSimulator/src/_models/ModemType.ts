import { CustomDataType } from "./CustomDataType";

export interface ModemType {
    customData?: CustomDataType;
    iccid?: string;
    imsi?: string;
  }
  