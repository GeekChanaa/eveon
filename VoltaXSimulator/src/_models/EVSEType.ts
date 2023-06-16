import { CustomDataType } from "./CustomDataType";

export interface EVSEType {
    customData?: CustomDataType;
    id: number;
    connectorId?: number;
  }