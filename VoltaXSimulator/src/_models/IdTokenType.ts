import { AdditionalInfoType } from "./AdditionalInfoType";
import { CustomDataType } from "./CustomDataType";
import { IdTokenEnumType } from "./_enums/IdTokenEnumType";

export interface IdTokenType {
    customData?: CustomDataType;
    additionalInfo?: AdditionalInfoType[];
    idToken: string;
    type: IdTokenEnumType;
  }