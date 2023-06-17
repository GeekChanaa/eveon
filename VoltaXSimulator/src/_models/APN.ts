import { CustomDataType } from "./CustomDataType";
import { APNAuthenticationEnumType } from "./_enums/APNAuthenticationEnumType";

export interface APN {
    customData?: CustomDataType;
    apn: string;
    apnUserName?: string;
    apnPassword?: string;
    simPin?: number;
    preferredNetwork?: string;
    useOnlyPreferredNetwork?: boolean;
    apnAuthentication: APNAuthenticationEnumType;
  }