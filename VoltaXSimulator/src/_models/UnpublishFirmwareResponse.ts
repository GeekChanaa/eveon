import { CustomDataType } from "./CustomDataType";
import { UnpublishFirmwareStatusEnumType } from "./_enums/UnpublishFirmwareStatusEnumType";

  export interface UnpublishFirmwareResponse {
    customData?: CustomDataType;
    status: UnpublishFirmwareStatusEnumType
  }
  