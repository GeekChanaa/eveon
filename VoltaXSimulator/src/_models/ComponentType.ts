import { CustomDataType } from "./CustomDataType";
import { EVSEType } from "./EVSEType";

  
  export interface ComponentType {
    customData?: CustomDataType;
    evse?: EVSEType;
    name: string;
    instance?: string;
  }