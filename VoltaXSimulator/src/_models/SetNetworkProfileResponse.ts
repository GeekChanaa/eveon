import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { SetNetworkProfileStatusEnum } from "./_enums/SetNetworkProfileStatusEnum";

  /**
   * OCPP 2.0.1 FINAL
   */
  export interface SetNetworkProfileResponse {
    customData?: CustomDataType;
    status: SetNetworkProfileStatusEnum;
    statusInfo?: StatusInfoType;
  }
  