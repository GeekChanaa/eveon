import { NoticeTypeEnum } from "src/_models/_enums/notice-type-enum";

export interface CreateNoticeDto{
    title : string,
    type : NoticeTypeEnum,
    isEmail : boolean,
    isSms : boolean,
    isPushNotification : boolean,
    text : string,
    forAdmins : boolean,
    forSupports : boolean,
    forPartners : boolean,
    forUsers : boolean,
    emailTemplate : any,
    [key: string]: any;
}