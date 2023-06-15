import { CustomDataType } from "./CustomDataType";
import { GetVariableDataType } from "./GetVariableDataType";

  


  
  export interface GetVariablesRequest {
    customData?: CustomDataType;
    getVariableData: GetVariableDataType[];
  }
  