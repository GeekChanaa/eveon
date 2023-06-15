import { AdditionalInfoType } from "./AdditionalInfoType";
import { IdTokenEnumType } from "./_enums/IdTokenEnumType";

export interface IdTokenType {
    customData?: CustomDataType;
    additionalInfo?: AdditionalInfoType[];
    idToken: string;
    type: IdTokenEnumType;
  }