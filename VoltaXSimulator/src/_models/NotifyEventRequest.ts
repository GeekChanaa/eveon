import { CustomDataType } from "./CustomDataType";
import { EventDataType } from "./EventDataType";

  
  export interface NotifyEventRequest {
    customData?: CustomDataType;
    generatedAt: string;
    tbc?: boolean;
    seqNo: number;
    eventData: EventDataType[];
  }
  