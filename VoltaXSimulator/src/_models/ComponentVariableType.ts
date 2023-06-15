import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { VariableType } from "./VariableType";

export interface ComponentVariableType {
    customData?: CustomDataType;
    component: ComponentType;
    variable?: VariableType;
  }