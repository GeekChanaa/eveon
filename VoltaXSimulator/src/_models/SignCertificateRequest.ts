import { CustomDataType } from "./CustomDataType";
import { CertificateSigningUseEnumType } from "./_enums/CertificateSigningUseEnumType";

export interface SignCertificateRequest {
    customData?: CustomDataType;
    csr: string;
    certificateType?: CertificateSigningUseEnumType;
}
