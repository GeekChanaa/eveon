import { CustomDataType } from "./CustomDataType";
import { FirmwareType } from "./FirmwareType";

  
  
  export interface UpdateFirmwareRequestType {
    customData?: CustomDataType;
    retries?: number;
    retryInterval?: number;
    requestId: number;
    firmware: FirmwareType;
  }
  