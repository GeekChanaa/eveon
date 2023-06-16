import { CustomDataType } from "./CustomDataType";

export interface UnlockConnectorRequest {
    customData?: CustomDataType;
    evseId: number;
    connectorId: number;
  }