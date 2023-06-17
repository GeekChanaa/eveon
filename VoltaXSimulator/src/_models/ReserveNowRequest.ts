import { CustomDataType } from "./CustomDataType";
import { IdTokenType } from "./IdTokenType";
import { ConnectorEnumType } from "./_enums/ConnectorEnumType";

  export interface ReserveNowRequest {
    customData?: CustomDataType;
    id: number;
    expiryDateTime: string;
    connectorType: ConnectorEnumType;
    idToken: IdTokenType;
    evseId?: number;
    groupIdToken?: IdTokenType;
  }
  