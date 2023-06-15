import { CustomDataType } from "./CustomDataType";
import { FirmwareStatusEnumType } from "./_enums/FirmwareStatusEnumType";

export interface FirmwareStatusNotificationRequest {
    customData?: CustomDataType;
    status: FirmwareStatusEnumType;
    requestId?: number;
}
