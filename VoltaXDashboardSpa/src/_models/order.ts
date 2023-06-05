import { Card } from "./card";
import { User } from "./user";

export interface Order {
    id: number;
    UserID?: number | null;
    CardID: number;
    Amount: number;
    RechargeDate: Date;
    User: User | null;
    Card: Card | null;
    [key: string]: any;
  }