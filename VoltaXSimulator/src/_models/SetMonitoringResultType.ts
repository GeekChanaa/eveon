import { Component } from "@angular/core";
import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { MonitorEnumType } from "./_enums/MonitorEnumType";
import { VariableType } from "./VariableType";
import { ComponentType } from "./ComponentType";
import { SetMonitoringStatusEnumType } from "./_enums/SetMonitoringStatusEnumType";

export interface SetMonitoringResultType {
    customData?: CustomDataType;
    id?: number;
    statusInfo?: StatusInfoType;
    status: SetMonitoringStatusEnumType;
    type: MonitorEnumType;
    component: ComponentType;
    variable: VariableType;
    severity: number;
  }