import { ReportCategoryEnum } from "./_enums/report-category";
import { ReportStatusEnum } from "./_enums/report-status";
import { ReportTypeEnum } from "./_enums/report-type";

export interface Report{
  ID : number,
  userID : number,
  connectorID : number,
  chargePointID : number,
  reportType : ReportTypeEnum,
  reportCategory : ReportCategoryEnum,
  issueDescription : string,
  status : ReportStatusEnum,
  reportDate : Date,
  resolvedDate : Date,
  [key: string]: any,
}