

export interface ChargingSession
{
  id: number;
  chargePointID: number;
  connectorID: number;
  startDate : Date;
  endDate : Date;
  connectorUptimeStatus : Date;
  [key: string]: any;
}