import { CustomDataType } from "./CustomDataType";
import { PublishFirmwareResponseStatusInfoType } from "./PublishFirmwareResponseStatusInfoType";
import { PublishFirmwareResponseStatusEnumType } from "./_enums/PublishFirmwareResponseStatusEnumType";

export interface PublishFirmwareResponse {
    customData?: CustomDataType;
    status: PublishFirmwareResponseStatusEnumType;
    statusInfo?: PublishFirmwareResponseStatusInfoType;
}
