import { DownloadRequestStatusEnum } from "./_enums/download-request-status-enum";
import { User } from "./user";

export interface UserInfoDownloadRequest{
    id : number,
    userID : number,
    requestTime : Date,
    status : DownloadRequestStatusEnum,
    user? : User,
    [key: string]: any;
}