import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { VariableMonitoringType } from "./VariableMonitoringType";
import { VariableType } from "./VariableType";

  
  export interface MonitoringDataType {
    customData?: CustomDataType;
    component: ComponentType;
    variable: VariableType;
    variableMonitoring: VariableMonitoringType[];
  }