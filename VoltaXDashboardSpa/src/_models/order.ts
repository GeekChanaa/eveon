import { Card } from "./card";
import { User } from "./user";

export interface Order {
    id: number;
    CardID: number;
    Amount: number;
    RechargeDate: Date;
    Card: Card | null;
    [key: string]: any;
  }