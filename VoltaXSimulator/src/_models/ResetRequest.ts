import { CustomDataType } from "./CustomDataType";
import { ResetEnumType } from "./_enums/ResetEnumType";

export interface ResetRequest {
    customData?: CustomDataType;
    type: ResetEnumType;
    evseId?: number;
}
