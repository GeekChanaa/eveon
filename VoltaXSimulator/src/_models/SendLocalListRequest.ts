import { AuthorizationDataType } from "./AuthorizationDataType";
import { CustomDataType } from "./CustomDataType";
import { UpdateEnumType } from "./_enums/UpdateEnumType";

/**
 * OCPP 2.0.1 FINAL
 */
export interface SendLocalListRequest {
    customData?: CustomDataType;
    localAuthorizationList: AuthorizationDataType[];
    versionNumber: number;
    updateType: UpdateEnumType;
}
