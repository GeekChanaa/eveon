import { CompositeScheduleType } from "./CompositeScheduleType";
import { CustomDataType } from "./CustomDataType";
import { StatusInfoType } from "./StatusInfoType";
import { GenericStatusEnum } from "./_enums/GenericStatusEnum";

  
 export  interface GetCompositeScheduleResponse {
    customData?: CustomDataType;
    status: GenericStatusEnum;
    statusInfo?: StatusInfoType;
    schedule?: CompositeScheduleType;
  }
  