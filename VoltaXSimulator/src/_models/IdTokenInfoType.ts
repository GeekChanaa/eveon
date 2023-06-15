import { CustomDataType } from "./CustomDataType";
import { IdTokenType } from "./IdTokenType";
import { MessageContentType } from "./MessageContentType";
import { AuthorizationStatusEnumType } from "./_enums/AuthorizationStatusEnumType";

export interface IdTokenInfoType {
    customData?: CustomDataType;
    status: AuthorizationStatusEnumType;
    cacheExpiryDateTime?: string;
    chargingPriority?: number;
    language1?: string;
    evseId?: number[];
    groupIdToken?: IdTokenType;
    language2?: string;
    personalMessage?: MessageContentType;
  }