export interface TransactionListDto {
  uid?: string | null;
  chargePointID?: string | null;
  connectorID: number;
  connectorName: string;
  chargingSessionID: number;
  startTagId?: string | null;
  startTime: Date;
  meterStart: number;
  startResult?: string | null;
  stopTagId?: string | null;
  stopTime?: Date | null;
  meterStop?: number | null;
  stopReason?: string | null;
  status?: string | null;
  amount: number;
  [key: string]: any;
}