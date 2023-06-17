import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GenericStatusEnumType } from "./_enums/GenericStatusEnumType";

export interface SignCertificateResponse {
    customData?: CustomDataType;
    status: GenericStatusEnumType;
    statusInfo?: StatusInfoType;
}
