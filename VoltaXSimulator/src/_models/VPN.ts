import { CustomDataType } from "./CustomDataType";
import { VPNEnumType } from "./_enums/VPNEnumType";

/**
   * VPN
   * urn:x-oca:ocpp:uid:2:233268
   * VPN Configuration settings
   */
export interface VPN {
    customData?: CustomDataType;
    server: string;
    user: string;
    group?: string;
    password: string;
    key: string;
    type: VPNEnumType;
  }