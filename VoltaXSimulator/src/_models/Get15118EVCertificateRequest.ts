import { CustomDataType } from "./CustomDataType";
import { CertificateActionEnumType } from "./_enums/CertificateActionEnumType";


  
  export interface Get15118EVCertificateRequest {
    customData?: CustomDataType;
    iso15118SchemaVersion: string;
    action: CertificateActionEnumType;
    exiRequest: string;
  }
  