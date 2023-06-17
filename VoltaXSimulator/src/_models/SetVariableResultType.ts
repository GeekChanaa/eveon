import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { VariableType } from "./VariableType";
import { AttributeEnumType } from "./_enums/AttributeEnumType";
import { SetVariableStatusEnumType } from "./_enums/SetVariableStatusEnumType";

export interface SetVariableResultType {
    customData?: CustomDataType;
    attributeType?: AttributeEnumType;
    attributeStatus: SetVariableStatusEnumType;
    attributeStatusInfo?: StatusInfoType;
    component: ComponentType;
    variable: VariableType;
  }