import { CustomDataType } from "./CustomDataType";
import { GetVariableResultType } from "./GetVariableResultType";


export interface GetVariablesResponse {
    customData?: CustomDataType;
    getVariableResult: GetVariableResultType[];
}
