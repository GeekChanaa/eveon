import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { DeleteCertificateStatusEnumType } from "./_enums/DeleteCertificateStatusEnumType";


  export interface DeleteCertificateResponse {
    customData?: CustomDataType;
    status: DeleteCertificateStatusEnumType;
    statusInfo?: StatusInfoType;
  }
  