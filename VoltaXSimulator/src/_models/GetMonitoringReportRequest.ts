import { ComponentVariableType } from "./ComponentVariableType";
import { CustomDataType } from "./CustomDataType";

  
  
  
  export interface GetMonitoringReportRequest {
    customData?: CustomDataType;
    componentVariable: ComponentVariableType[];
    requestId: number;
    monitoringCriteria: MonitoringCriterionEnumType[];
  }
  