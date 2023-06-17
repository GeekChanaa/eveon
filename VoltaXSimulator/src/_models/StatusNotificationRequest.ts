import { CustomDataType } from "./CustomDataType";
import { ConnectorStatusEnumType } from "./_enums/ConnectorStatusEnumType";

  /**
   * OCPP 2.0.1 FINAL
   */
  export interface StatusNotificationRequest {
    customData?: CustomDataType;
    timestamp: string;
    connectorStatus: ConnectorStatusEnumType;
    evseId: number;
    connectorId: number;
  }
  