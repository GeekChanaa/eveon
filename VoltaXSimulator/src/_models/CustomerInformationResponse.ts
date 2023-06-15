import { CustomerInformationStatusEnumType } from "./_enums/CustomerInformationStatusEnumType";
import { StatusInfoType } from "./StatusInfoType";

export interface CustomerInformationResponse {
    customData?: {
        vendorId: string;
    };
    status: CustomerInformationStatusEnumType;
    statusInfo?: StatusInfoType;
}



