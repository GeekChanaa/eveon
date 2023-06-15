import { CustomDataType } from "./CustomDataType";
import { IdTokenInfoType } from "./IdTokenInfoType";
import { AuthorizeCertificateStatusEnumType } from "./_enums/AuthorizeCertificateStatusEnumType";

export interface AuthorizeResponse {
    customData?: CustomDataType;
    idTokenInfo: IdTokenInfoType;
    certificateStatus?: AuthorizeCertificateStatusEnumType;
  }
  

  
 

 
  