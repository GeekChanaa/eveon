import { Connector } from "./connector";

export interface ConnectorTarif {
    id: number;
    connectorID: number;
    unit: string;
    quantity: string;
    currency: string;
    connector: Connector;
    [key: string]: any;
  }
  