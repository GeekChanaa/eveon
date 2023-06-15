import { CustomDataType } from "./CustomDataType";
import { MonitorEnumType } from "./_enums/MonitorEnumType";

export interface VariableMonitoringType {
    customData?: CustomDataType;
    id: number;
    transaction: boolean;
    value: number;
    type: MonitorEnumType;
    severity: number;
  }