import { CustomDataType } from "./CustomDataType";

  export interface PublishFirmwareRequest {
    customData?: CustomDataType;
    location: string;
    retries?: number;
    checksum: string;
    requestId: number;
    retryInterval?: number;
  }
  