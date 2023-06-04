import { ChargingStation } from "./charging-station";

export interface ChargePoint {
    id: number;
    ChargingStationID: number;
    Network: string;
    Timezone: string;
    LastConnectTime: Date;
    OnlineTime: Date;
    ChargingStation: ChargingStation | null;
    [key: string]: any;
}