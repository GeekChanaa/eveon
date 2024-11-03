
export interface ConnectorUptime {
  id: number;
  connectorID: number;
  startDate : Date;
  endDate : Date;
  connectorUptimeStatus : Date;
  [key: string]: any;
}
