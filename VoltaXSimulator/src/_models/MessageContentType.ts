import { CustomDataType } from "./CustomDataType";
import { MessageFormatEnumType } from "./_enums/MessageFormatEnumType";

export interface MessageContentType {
    customData?: CustomDataType;
    format: MessageFormatEnumType;
    language?: string;
    content: string;
  }
  