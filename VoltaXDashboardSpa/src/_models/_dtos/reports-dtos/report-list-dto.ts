import { ReportCategoryEnum } from "src/_models/_enums/report-category";
import { ReportStatusEnum } from "src/_models/_enums/report-status";
import { ReportTypeEnum } from "src/_models/_enums/report-type";


export interface ReportListDto{
  id : number,
  userName : string,
  connectorName : string,
  chargePointName : string,
  reportType : ReportTypeEnum,
  reportCategory : ReportCategoryEnum,
  issueDescription : string,
  status : ReportStatusEnum,
  reportDate : Date,
  resolvedDate : Date,
  [key: string]: any,
}