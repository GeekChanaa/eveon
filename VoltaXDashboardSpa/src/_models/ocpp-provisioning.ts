export type AttributeType = 'Actual' | 'Target' | 'MinSet' | 'MaxSet';
export const ATTRIBUTE_TYPES: AttributeType[] = ['Actual', 'Target', 'MinSet', 'MaxSet'];

/** One OCPP 2.0.1 device-model setting sent with SetVariables. */
export interface ProvisioningVariable {
  groupName: string;
  componentName: string;
  componentInstance?: string | null;
  evseId?: number | null;
  connectorId?: number | null;
  variableName: string;
  variableInstance?: string | null;
  attributeType: AttributeType;
  value: string;
  description?: string | null;
}

export type VariableSupport = 'Writable' | 'ReadOnly' | 'NotReported' | 'Unknown';

export interface ProvisioningPlanVariable extends ProvisioningVariable {
  support: VariableSupport;
  currentValue?: string | null;
  mutability?: string | null;
  dataType?: string | null;
  unit?: string | null;
  valuesList?: string | null;
  minLimit?: number | null;
  maxLimit?: number | null;
}

export interface ProvisioningPlan {
  chargePointId: string;
  discoveryStatus: 'Completed' | 'NotSupported' | 'Rejected' | 'TimedOut' | 'Failed';
  discoveryMessage?: string | null;
  reportedVariableCount: number;
  itemsPerMessage: number;
  variables: ProvisioningPlanVariable[];
}

export interface ProvisioningVariableResult extends ProvisioningVariable {
  status: string;
  statusReason?: string | null;
}

export interface ProvisioningResult {
  provisioned: boolean;
  acceptedCount: number;
  failedCount: number;
  rebootRequired: boolean;
  followUp?: string | null;
  message?: string | null;
  results: ProvisioningVariableResult[];
}

export interface PendingProvisioningChargePoint {
  id: number;
  chargePointId: string;
  stationName?: string | null;
  vendorName?: string | null;
  modelName?: string | null;
  serialNumber?: string | null;
  firmwareVersion?: string | null;
}

export interface OcppDefaultVariable extends ProvisioningVariable {
  id: number;
  enabled: boolean;
  sortOrder: number;
}

/** "TxCtrlr.MessageAttempts[TransactionEvent]" style label. */
export function variableLabel(v: ProvisioningVariable): string {
  const component = v.componentName + (v.componentInstance ? `[${v.componentInstance}]` : '')
    + (v.evseId != null ? ` · EVSE ${v.evseId}${v.connectorId != null ? '/' + v.connectorId : ''}` : '');
  return `${component}.${v.variableName}${v.variableInstance ? `[${v.variableInstance}]` : ''}`;
}

/** Plain copy with only the fields the API accepts. */
export function toVariableInput(v: ProvisioningVariable): ProvisioningVariable {
  return {
    groupName: v.groupName,
    componentName: v.componentName,
    componentInstance: v.componentInstance || null,
    evseId: v.evseId ?? null,
    connectorId: v.evseId != null ? v.connectorId ?? null : null,
    variableName: v.variableName,
    variableInstance: v.variableInstance || null,
    attributeType: v.attributeType,
    value: v.value ?? '',
    description: v.description || null
  };
}
