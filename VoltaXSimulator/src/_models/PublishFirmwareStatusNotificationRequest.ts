import { PublishFirmwareStatusEnumType } from "./_enums/PublishFirmwareStatusEnumType";
import { CustomDataType } from "./CustomDataType";


export interface PublishFirmwareStatusNotificationRequest {
    customData?: CustomDataType;
    status: PublishFirmwareStatusEnumType;
    location?: string[];
    requestId?: number;
}
