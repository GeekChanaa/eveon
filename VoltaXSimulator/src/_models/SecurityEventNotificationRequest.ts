import { CustomDataType } from "./CustomDataType";

export interface SecurityEventNotificationRequest {
    customData?: CustomDataType;
    type: string;
    timestamp: string;
    techInfo?: string;
}
