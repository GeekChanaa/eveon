import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { VariableType } from "./VariableType";
import { EventNotificationEnumType } from "./_enums/EventNotificationEnumType";
import { EventTriggerEnumType } from "./_enums/EventTriggerEnumType";

export interface EventDataType {
    customData?: CustomDataType;
    eventId: number;
    timestamp: string;
    trigger: EventTriggerEnumType;
    cause?: number;
    actualValue: string;
    techCode?: string;
    techInfo?: string;
    cleared?: boolean;
    transactionId?: string;
    component: ComponentType;
    variableMonitoringId?: number;
    eventNotificationType: EventNotificationEnumType;
    variable: VariableType;
  }