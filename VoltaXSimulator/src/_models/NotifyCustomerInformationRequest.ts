import { CustomDataType } from "./CustomDataType";

export interface NotifyCustomerInformationRequest {
    customData?: CustomDataType;
    data: string;
    tbc?: boolean;
    seqNo: number;
    generatedAt: string;
    requestId: number;
}
