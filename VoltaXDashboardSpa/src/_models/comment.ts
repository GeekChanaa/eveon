import { ChargingStation } from "./charging-station";
import { User } from "./user";

export interface Comment {
    id: number;
    UserID: number;
    Rating: number;
    Text: string;
    ChargingStationID: number;
    PointID: number;
    CommentTime: Date;
    [key: string]: any;
}