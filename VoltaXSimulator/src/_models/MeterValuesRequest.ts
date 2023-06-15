import { CustomDataType } from "./CustomDataType";
import { MeterValueType } from "./MeterValueType";

export interface MeterValuesRequest {
    customData?: CustomDataType;
    evseId: number;
    meterValue: MeterValueType[];
}
