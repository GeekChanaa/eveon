import { CustomDataType } from "./CustomDataType";
import { OCSPRequestDataType } from "./OCSPRequestDataType";


export interface GetCertificateStatusRequest {
    customData?: CustomDataType;
    ocspRequestData: OCSPRequestDataType;
}
