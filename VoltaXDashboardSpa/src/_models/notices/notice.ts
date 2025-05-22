import { NoticeTypeEnum } from "../_enums/notice-type-enum"

export interface Notice{
    type : NoticeTypeEnum,
    emailTemplatePath : string,
    isEmail : boolean,
    isSms : boolean,
    isPushNotification : boolean,
    text : string,
    forAdmins : boolean,
    forSupports : boolean,
    forPartners : boolean,
    forUsers : boolean
    [key: string]: any;
}