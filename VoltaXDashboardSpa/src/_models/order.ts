import { CarCharger } from "./car-charger";
import { Card } from "./card";
import { User } from "./user";

export interface Order {
    id: number;
    UserID?: number | null;
    CardID: number;
    CarChargerID: number;
    StartTime: Date;
    StopTime: Date;
    Duration: number;
    StopReason: string;
    User: User;
    Card: Card;
    CarCharger: CarCharger;
  }