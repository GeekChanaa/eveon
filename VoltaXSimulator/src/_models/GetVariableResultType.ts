import { ComponentType } from "./ComponentType";
import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { VariableType } from "./VariableType";
import { AttributeEnumType } from "./_enums/AttributeEnumType";
import { GetVariableStatusEnumType } from "./_enums/GetVariableStatusEnumType";

export interface GetVariableResultType {
    customData?: CustomDataType;
    attributeStatusInfo?: StatusInfoType;
    attributeStatus: GetVariableStatusEnumType;
    attributeType: AttributeEnumType;
    attributeValue: string;
    component: ComponentType;
    variable: VariableType;
  }