import { ReportCategoryEnum } from "./_enums/report-category";
import { ReportCriticality } from "./_enums/report-criticality";
import { ReportStatusEnum } from "./_enums/report-status";

export interface SystemReport {
  id:number,
  reportCategory : ReportCategoryEnum,
  userID : number,
  cardID : number,
  connectorID : number,
  chargePointID : number,
  resolvedByID : number,
  assignedID : number,
  issueDescription : string,
  isEmail : boolean,
  isNotification : boolean,
  status: ReportStatusEnum,
  criticality: ReportCriticality,
  [key: string]: any,
}