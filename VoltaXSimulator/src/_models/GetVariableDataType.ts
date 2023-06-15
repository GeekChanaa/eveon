import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { VariableType } from "./VariableType";
import { AttributeEnumType } from "./_enums/AttributeEnumType";

export interface GetVariableDataType {
    customData?: CustomDataType;
    attributeType?: AttributeEnumType;
    component: ComponentType;
    variable: VariableType;
  }