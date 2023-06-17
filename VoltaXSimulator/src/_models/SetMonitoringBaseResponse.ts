import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GenericDeviceModelStatusEnumType } from "./_enums/GenericDeviceModelStatusEnumType";



export interface SetMonitoringBaseResponse {
    customData?: CustomDataType;
    status: GenericDeviceModelStatusEnumType;
    statusInfo?: StatusInfoType;
}
