import { CustomDataType } from "./CustomDataType";
export  interface PublishFirmwareResponseStatusInfoType {
    customData?: CustomDataType;
    reasonCode: string;
    additionalInfo?: string;
  }