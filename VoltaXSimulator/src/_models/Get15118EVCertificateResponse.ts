import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { Iso15118EVCertificateStatusEnumType } from "./_enums/Iso15118EVCertificateStatusEnumType";

  export interface Get15118EVCertificateResponse {
    customData?: CustomDataType;
    status: Iso15118EVCertificateStatusEnumType;
    statusInfo?: StatusInfoType;
    exiResponse: string;
  }
  