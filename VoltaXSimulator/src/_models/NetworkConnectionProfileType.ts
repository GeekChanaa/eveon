import { APN } from "./APN";
import { CustomDataType } from "./CustomDataType";
import { VPN } from "./VPN";
import { OCPPInterfaceEnumType } from "./_enums/OCPPInterfaceEnumType";
import { OCPPTransportEnumType } from "./_enums/OCPPTransportEnumType";
import { OCPPVersionEnumType } from "./_enums/OCPPVersionEnumType";

export interface NetworkConnectionProfileType {
    customData?: CustomDataType;
    apn?: APN;
    ocppVersion: OCPPVersionEnumType;
    ocppTransport: OCPPTransportEnumType;
    ocppCsmsUrl: string;
    messageTimeout: number;
    securityProfile: number;
    ocppInterface: OCPPInterfaceEnumType;
    vpn?: VPN;
  } 