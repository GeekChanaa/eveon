import { CustomDataType } from "./CustomDataType";

export interface LogParametersType {
    customData?: CustomDataType;
    remoteLocation: string;
    oldestTimestamp?: string;
    latestTimestamp?: string;
  }