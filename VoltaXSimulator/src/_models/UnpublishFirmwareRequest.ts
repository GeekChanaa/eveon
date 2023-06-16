import { CustomDataType } from "./CustomDataType";

  export interface UnpublishFirmwareRequest {
    customData?: CustomDataType;
    checksum: string;
  }
  