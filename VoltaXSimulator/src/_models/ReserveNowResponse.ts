import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { ReserveNowStatusEnumType } from "./_enums/ReserveNowStatusEnumType";

export interface ReserveNowResponse {
    customData?: CustomDataType;
    status: ReserveNowStatusEnumType;
    statusInfo?: StatusInfoType;
}
