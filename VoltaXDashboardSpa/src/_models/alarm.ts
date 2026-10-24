export type AlarmType = 'ConnectorFault' | 'DeviceEvent' | 'Offline' | 'BootLoop';
export type AlarmSeverity = 'Warning' | 'Minor' | 'Major' | 'Critical';
export type AlarmStatus = 'Open' | 'Acknowledged' | 'Resolved' | 'Cleared';
export type AlarmStatusFilter = 'active' | 'open' | 'acknowledged' | 'resolved' | 'cleared' | 'closed' | 'all';

export const ALARM_SEVERITIES: AlarmSeverity[] = ['Critical', 'Major', 'Minor', 'Warning'];
export const ALARM_TYPES: AlarmType[] = ['ConnectorFault', 'DeviceEvent', 'Offline', 'BootLoop'];
export const ALARM_STATUS_FILTERS: AlarmStatusFilter[] = ['active', 'open', 'acknowledged', 'resolved', 'cleared', 'closed', 'all'];

/** An alarm as api/Alarm returns it. */
export interface Alarm {
  id: number;
  type: AlarmType;
  code: string;
  severity: AlarmSeverity;
  status: AlarmStatus;
  description: string;
  details: string | null;
  chargePointID: number;
  chargePointIdentity: string;
  chargingStationID: number | null;
  stationName: string | null;
  evseId: number | null;
  connectorNumber: number | null;
  connectorID: number | null;
  firstSeenAt: string;
  lastSeenAt: string;
  count: number;
  acknowledgedAt: string | null;
  acknowledgedBy: string | null;
  assignedToID: number | null;
  assignedTo: string | null;
  assignedAt: string | null;
  resolvedAt: string | null;
  resolvedBy: string | null;
  resolutionComment: string | null;
  clearedAt: string | null;
}

export interface AlarmPage {
  items: Alarm[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AlarmSummary {
  active: number;
  critical: number;
  major: number;
  minor: number;
  warning: number;
  unassigned: number;
  assignedToMe: number;
}

export interface AlarmAssignee {
  id: number;
  name: string;
  email: string;
}

export interface AlarmFilter {
  status: AlarmStatusFilter;
  severity: AlarmSeverity | '';
  type: AlarmType | '';
  chargingStationID: number | null;
  chargePointID?: number | null;
  /** '' anyone, 'me', 'none' (unassigned). */
  assigned: '' | 'me' | 'none';
  from: string | null;
  to: string | null;
  search: string;
  sort: '' | 'severity' | 'firstSeen' | 'count';
  page: number;
  pageSize: number;
}

/** What the API pushes on the charger hub (group "#alarms", method "AlarmChanged"). */
export interface AlarmChange {
  change: 'Raised' | 'Updated' | 'Cleared' | 'Acknowledged' | 'Assigned' | 'Resolved';
  alarm: Alarm;
}

export function isActiveAlarm(alarm: Pick<Alarm, 'status'>): boolean {
  return alarm.status === 'Open' || alarm.status === 'Acknowledged';
}
