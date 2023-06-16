import { CustomDataType } from "./CustomDataType";

export interface FirmwareType {
    customData?: CustomDataType;
    location: string;
    retrieveDateTime: string;
    installDateTime?: string;
    signingCertificate?: string;
    signature?: string;
  }