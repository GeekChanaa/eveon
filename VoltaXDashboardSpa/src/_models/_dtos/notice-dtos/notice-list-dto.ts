import { NoticeTypeEnum } from "src/_models/_enums/notice-type-enum";

export interface NoticeListDto{
    title : string,
    type : NoticeTypeEnum,
    isEmail : boolean,
    isSms : boolean,
    isPushNotification : boolean,
    forAdmins : boolean,
    forSupports : boolean,
    forPartners : boolean,
    forUsers : boolean,
    [key: string]: any;
}