import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { InstallCertificateStatusEnum } from "./_enums/InstallCertificateStatusEnum";

  export interface InstallCertificateResponse {
    customData?: CustomDataType;
    status: InstallCertificateStatusEnum;
    statusInfo?: StatusInfoType;
  }
  