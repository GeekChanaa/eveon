import { CustomDataType } from "./CustomDataType";

  
  export interface CostUpdatedRequest {
    customData?: CustomDataType;
    totalCost: number;
    transactionId: string;
  }
  