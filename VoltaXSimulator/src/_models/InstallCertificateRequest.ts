import { CustomDataType } from "./CustomDataType";
import { InstallCertificateUseEnumType } from "./_enums/InstallCertificateUseEnumType";

export interface InstallCertificateRequest {
    customData?: CustomDataType;
    certificateType: InstallCertificateUseEnumType;
    certificate: string;
  }
  