import { CustomDataType } from "./CustomDataType";
import { MonitoringDataType } from "./MonitoringDataType";

  
  export interface NotifyMonitoringReportRequest {
    customData?: CustomDataType;
    monitor: MonitoringDataType[];
    requestId: number;
    tbc?: boolean;
    seqNo: number;
    generatedAt: string;
  }
  