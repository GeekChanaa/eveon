import { CustomDataType } from "./CustomDataType";

export interface StatusInfoType {
    customData?: CustomDataType;
    reasonCode: string;
    additionalInfo?: string;
  }