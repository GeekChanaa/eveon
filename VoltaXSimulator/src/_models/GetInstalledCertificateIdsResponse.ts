import { CertificateHashDataChain } from "./CertificateHashDataChain";
import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GetInstalledCertificateStatusEnum } from "./_enums/GetInstalledCertificateStatusEnum";
  


  export interface GetInstalledCertificateIdsResponse {
    customData?: CustomDataType;
    status: GetInstalledCertificateStatusEnum;
    statusInfo?: StatusInfoType;
    certificateHashDataChain?: CertificateHashDataChain[];
  }
  