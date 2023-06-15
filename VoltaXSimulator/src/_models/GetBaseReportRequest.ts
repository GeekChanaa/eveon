import { CustomDataType } from "./CustomDataType";
import { ReportBaseEnum } from "./_enums/ReportBaseEnum";

  export interface GetBaseReportRequest {
    customData: CustomDataType;
    requestId: number;
    reportBase: ReportBaseEnum;
  }
  