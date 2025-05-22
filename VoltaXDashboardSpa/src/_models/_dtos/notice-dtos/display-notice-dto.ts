import { NoticeTypeEnum } from "src/_models/_enums/notice-type-enum";

export interface DisplayNoticeDto{
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
    emailTemplatePath : string,
    [key: string]: any;
}