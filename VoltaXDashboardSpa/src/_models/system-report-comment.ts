export interface SystemReportComment{
  id : number,
  content : string,
  userName : string,
  systemReportID : number,
  systemReport : any,
  systemReportCommentImages : any[],
  [key: string]: any,
}