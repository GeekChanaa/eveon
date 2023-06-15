import { CustomDataType } from "./CustomDataType";
import { GetCertificateIdUseEnum } from "./_enums/GetCertificateIdUseEnum";



export interface GetInstalledCertificateIdsRequest {
    customData?: CustomDataType;
    certificateType?: GetCertificateIdUseEnum[];
}
