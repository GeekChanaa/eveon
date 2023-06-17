import { CustomDataType } from "./CustomDataType";
import { SetVariableResultType } from "./SetVariableResultType";


  export interface SetVariablesResponse {
    customData?: CustomDataType;
    setVariableResult: SetVariableResultType[];
  }
  