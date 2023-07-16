import { Card } from "./card";
import { User } from "./user";

export interface Order {
    id: number;
    cardID: number;
    amount: number;
    rechargeDate: Date;
    card: Card | null;
    [key: string]: any;
  }