import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GenericStatusEnumType } from "./_enums/GenericStatusEnumType";

export interface SetMonitoringLevelResponse {
    customData?: CustomDataType;
    status: GenericStatusEnumType;
    statusInfo?: StatusInfoType;
}
