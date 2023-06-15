import { CustomDataType } from "./CustomDataType";
import { SampledValueType } from "./SampledValueType";

export interface MeterValueType {
    customData?: CustomDataType;
    sampledValue: SampledValueType[];
    timestamp: string;
  }