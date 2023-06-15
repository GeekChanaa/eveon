import { CustomDataType } from "./CustomDataType";
import { EVSEType } from "./EVSEType";
import { OperationalStatusEnumType } from "./_enums/OperationalStatusEnumType";

export interface ChangeAvailabilityRequest {
    customData?: CustomDataType;
    evse?: EVSEType;
    operationalStatus: OperationalStatusEnumType;
}
