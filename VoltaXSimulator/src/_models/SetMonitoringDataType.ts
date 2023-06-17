import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { VariableType } from "./VariableType";
import { MonitorEnumType } from "./_enums/MonitorEnumType";

export interface SetMonitoringDataType {
    customData?: CustomDataType;
    id?: number;
    transaction?: boolean;
    value: number;
    type: MonitorEnumType;
    severity: number;
    component: ComponentType;
    variable: VariableType;
  }