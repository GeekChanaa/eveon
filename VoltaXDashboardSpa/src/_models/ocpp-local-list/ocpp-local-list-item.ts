import { IdTokenEnumType } from "../ocpp-models/AuthorizeRequest";
import { AuthorizationStatusEnumType } from "../ocpp-models/AuthorizeResponse";


export interface OCPPLocalListItem {
    id: number;
    token : string,
    tokenType  : IdTokenEnumType,
    tokenStatus : AuthorizationStatusEnumType,
}