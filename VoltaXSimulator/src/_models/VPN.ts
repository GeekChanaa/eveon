import { CustomDataType } from "./CustomDataType";
import { VPNEnumType } from "./_enums/VPNEnumType";

export interface VPN {
    customData?: CustomDataType;
    server: string;
    user: string;
    group?: string;
    password: string;
    key: string;
    type: VPNEnumType;
  }