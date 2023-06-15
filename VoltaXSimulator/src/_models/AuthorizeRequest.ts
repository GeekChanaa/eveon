import { CustomDataType } from "./CustomDataType";
import { IdTokenType } from "./IdTokenType";
import { OCSPRequestDataType } from "./OCSPRequestDataType";

interface AuthorizeRequest {
    customData?: CustomDataType;
    idToken: IdTokenType;
    certificate?: string;
    iso15118CertificateHashData?: OCSPRequestDataType[];
  }
  
  
  
  
  

  

  