import { ProvisioningVariable, ProvisioningVariableResult } from './ocpp-provisioning';

export type ConfigurationMethod = 'Automatic' | 'Manual' | 'Skipped' | 'Legacy';
export type ReportBase = 'FullInventory' | 'ConfigurationInventory' | 'SummaryInventory';

/** A charge point's connection and configuration state. */
export interface ChargePointConfigurationStatus {
  id: number;
  chargePointId: string;
  stationName?: string | null;
  vendorName?: string | null;
  modelName?: string | null;
  serialNumber?: string | null;
  firmwareVersion?: string | null;
  isOnline: boolean;
  /** Our system sent it its configuration (automatic or manual setup). */
  isConfigured: boolean;
  /** Accepted on BootNotification. False: it is kept Pending and cannot charge. */
  isAccepted: boolean;
  configurationMethod?: ConfigurationMethod | null;
  configurationStatus?: 'Provisioned' | 'AwaitingReboot' | null;
  configuredAt?: string | null;
  configuredBy?: string | null;
  acceptedCount: number;
  failedCount: number;
  reportedVariableCount: number;
  lastReportAt?: string | null;
}

export interface DeviceModelAttribute {
  type: 'Actual' | 'Target' | 'MinSet' | 'MaxSet';
  value?: string | null;
  mutability: 'ReadOnly' | 'WriteOnly' | 'ReadWrite';
  persistent: boolean;
  constant: boolean;
}

/** One component/variable of the charger's OCPP 2.0.1 device model. */
export interface DeviceModelVariable {
  componentName: string;
  componentInstance?: string | null;
  evseId?: number | null;
  connectorId?: number | null;
  variableName: string;
  variableInstance?: string | null;
  dataType?: string | null;
  unit?: string | null;
  minLimit?: number | null;
  maxLimit?: number | null;
  valuesList?: string | null;
  supportsMonitoring: boolean;
  updatedAt: string;
  attributes: DeviceModelAttribute[];
}

export interface DeviceModelReportResult {
  reportBase: ReportBase;
  status: 'Completed' | 'NotSupported' | 'Rejected' | 'TimedOut' | 'Failed';
  message?: string | null;
  reportedVariableCount: number;
}

export interface DeviceModelSetResult {
  anyAnswer: boolean;
  acceptedCount: number;
  failedCount: number;
  rebootRequired: boolean;
  results: ProvisioningVariableResult[];
}

/** "EVSE 1 / 2" style label for a component's location. */
export function evseLabel(v: { evseId?: number | null; connectorId?: number | null }): string {
  if (v.evseId == null) return '';
  return `EVSE ${v.evseId}${v.connectorId != null ? ' · connector ' + v.connectorId : ''}`;
}

/** Stable identity of a component (name, instance, EVSE). */
export function componentKey(v: { componentName: string; componentInstance?: string | null; evseId?: number | null; connectorId?: number | null }): string {
  return [v.componentName, v.componentInstance ?? '', v.evseId ?? '', v.connectorId ?? ''].join('|');
}

/** Stable identity of one attribute of a variable. */
export function attributeKey(v: DeviceModelVariable | ProvisioningVariable, type: string): string {
  return [componentKey(v), v.variableName, v.variableInstance ?? '', type].join('|');
}
