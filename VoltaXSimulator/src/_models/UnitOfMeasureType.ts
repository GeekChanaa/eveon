import { CustomDataType } from "./CustomDataType";

 
  
  export interface UnitOfMeasureType {
    customData?: CustomDataType;
    unit: string;
    multiplier: number;
  }