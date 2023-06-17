import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { VariableType } from "./VariableType";
import { AttributeEnumType } from "./_enums/AttributeEnumType";

export interface SetVariableDataType {
    customData?: CustomDataType;
    attributeType?: AttributeEnumType;
    attributeValue: string;
    component: ComponentType;
    variable: VariableType;
}