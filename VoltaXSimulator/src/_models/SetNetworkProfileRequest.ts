import { CustomDataType } from "./CustomDataType";
import { NetworkConnectionProfileType } from "./NetworkConnectionProfileType";

export interface SetNetworkProfileRequest {
    customData?: CustomDataType;
    configurationSlot: number;
    connectionData: NetworkConnectionProfileType;
}
