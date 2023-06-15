import { CustomDataType } from "./CustomDataType";
import { CertificateHashDataType } from "./_enums/CertificateHashDataType";
  
  export interface DeleteCertificateRequest {
    customData?: CustomDataType;
    certificateHashData: CertificateHashDataType;
  }
  