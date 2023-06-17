import { CustomDataType } from "./CustomDataType";
import { SetVariableDataType } from "./SetVariableDataType";

  export interface SetVariablesRequest {
    customData?: CustomDataType;
    setVariableData: SetVariableDataType[];
  }
  