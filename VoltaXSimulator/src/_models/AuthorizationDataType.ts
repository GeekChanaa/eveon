import { CustomDataType } from "./CustomDataType";
import { IdTokenInfoType } from "./IdTokenInfoType";
import { IdTokenType } from "./IdTokenType";

export interface AuthorizationDataType {
    customData?: CustomDataType;
    idToken: IdTokenType;
    idTokenInfo?: IdTokenInfoType;
}