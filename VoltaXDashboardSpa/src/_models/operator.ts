/** Shapes of the operator search endpoints (api/ChargingSession/Search, api/Transaction/Search). */

export interface OperatorSessionFilter {
  from?: string | null;
  to?: string | null;
  status?: '' | 'active' | 'completed';
  search?: string | null;
  stationId?: number | null;
  chargePointId?: number | null;
  userId?: number | null;
  page?: number;
  pageSize?: number;
}

export interface OperatorSession {
  id: number;
  chargePointId: number | null;
  stationName: string | null;
  chargePointName: string | null;
  connector: string | null;
  userId: number | null;
  userName: string | null;
  cardLast4: string | null;
  startDate: string;
  endDate: string | null;
  chargedMinutes: number;
  idleMinutes: number;
  chargedKwhs: number;
  total: number;
  stoppedReason: string;
  status: string;
  active: boolean;
  billed: boolean;
}

export interface OperatorTransactionFilter {
  from?: string | null;
  to?: string | null;
  status?: '' | 'Current' | 'Ended' | 'Forbidden';
  search?: string | null;
  chargePointId?: number | null;
  page?: number;
  pageSize?: number;
}

export interface OperatorTransaction {
  id: number;
  uid: string | null;
  chargingSessionId: number;
  chargePointId: number | null;
  chargePointName: string | null;
  stationName: string | null;
  connector: string | null;
  startTime: string;
  stopTime: string | null;
  meterStart: number;
  meterStop: number | null;
  idTag: string | null;
  startResult: string | null;
  stopReason: string | null;
  status: 'Current' | 'Ended' | 'Forbidden' | null;
  amount: number;
}
