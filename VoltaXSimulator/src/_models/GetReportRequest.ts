import { ComponentVariableType } from "./ComponentVariableType";
import { CustomDataType } from "./CustomDataType";
import { ComponentCriterionEnumType } from "./_enums/ComponentCriterionEnumType";
export interface GetReportRequest {
    customData?: CustomDataType;
    componentVariable: ComponentVariableType[];
    requestId?: number;
    componentCriteria: ComponentCriterionEnumType[];
}




