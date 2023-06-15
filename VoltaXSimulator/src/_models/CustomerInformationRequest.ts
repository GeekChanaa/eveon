import { CustomDataType } from "./CustomDataType";
import { IdTokenType } from "./IdTokenType";
import { CertificateHashDataType } from "./_enums/CertificateHashDataType";
  
  
  

  
  export interface CustomerInformationRequest {
    customData?: CustomDataType;
    customerCertificate?: CertificateHashDataType;
    idToken?: IdTokenType;
    requestId: number;
    report: boolean;
    clear: boolean;
    customerIdentifier?: string;
  }
  