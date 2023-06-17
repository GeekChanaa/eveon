import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ResetStatusEnumType } from "./_enums/ResetStatusEnumType";


export interface ResetResponse {
    customData?: CustomDataType;
    status: ResetStatusEnumType;
    statusInfo?: StatusInfoType;
}
