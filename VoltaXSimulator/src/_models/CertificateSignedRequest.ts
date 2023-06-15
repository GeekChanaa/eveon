import { CustomDataType } from "./CustomDataType";
import { CertificateSigningUseEnumType } from "./_enums/CertificateSigningUseEnumType";

  export interface CertificateSignedRequest {
    customData?: CustomDataType;
    certificateChain: string;
    certificateType?: CertificateSigningUseEnumType;
  }
  