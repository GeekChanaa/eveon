import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GetCertificateStatusEnum } from "./_enums/GetCertificateStatusEnum";

  
  export interface GetCertificateStatusResponse {
    customData?: CustomDataType;
    status: GetCertificateStatusEnum;
    statusInfo?: StatusInfoType;
    ocspResult?: string;
  }
  