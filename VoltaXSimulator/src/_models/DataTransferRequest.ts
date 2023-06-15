import { CustomDataType } from "./CustomDataType";

export interface DataTransferRequest {
    customData?: CustomDataType
    messageId?: string;
    data?: any;
    vendorId: string;
  }
  