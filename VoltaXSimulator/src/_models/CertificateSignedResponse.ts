import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { CertificateSignedStatusEnumType } from "./_enums/CertificateSignedStatusEnumType";

export interface CertificateSignedResponse {
    customData?: CustomDataType;
    status: CertificateSignedStatusEnumType;
    statusInfo?: StatusInfoType;
}
