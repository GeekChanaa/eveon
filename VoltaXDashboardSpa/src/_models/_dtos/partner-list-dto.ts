import { PartnerTypeEnum } from "../_enums/partner-enum-type";

export interface PartnerListDto{
    id : number,
    name : string,
    description : string,
    type: PartnerTypeEnum,
    address: string,
    logoUrl : string,
    [key: string]: any;
}