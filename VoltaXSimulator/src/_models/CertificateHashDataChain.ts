import { CertificateHashData } from "./CertificateHashData";
import { CustomDataType } from "./CustomDataType";
import { GetCertificateIdUseEnum } from "./_enums/GetCertificateIdUseEnum";

export interface CertificateHashDataChain {
    customData?: CustomDataType;
    certificateHashData: CertificateHashData;
    certificateType: GetCertificateIdUseEnum;
    childCertificateHashData?: CertificateHashData[];
  }