import { RatingReportCategoryEnum } from "./_enums/rating-report-category-enum";
import { ReportStatusEnum } from "./_enums/report-status";
import { Rating } from "./rating";
import { User } from "./user";

export interface RatingReport{
  id : number,
  userID : number,
  ratingID : number,
  reportCategory : RatingReportCategoryEnum,
  issueDescription : string,
  status : ReportStatusEnum,
  user?: User,
  rating? : Rating,
  [key: string]: any;
}
