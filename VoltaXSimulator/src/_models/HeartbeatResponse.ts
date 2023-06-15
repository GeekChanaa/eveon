import { CustomDataType } from "./CustomDataType";

export interface HeartbeatResponse {
    customData?: CustomDataType;
    currentTime: string;
  }
  