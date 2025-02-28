import { RechargeOrderStatus } from "./_enums/recharge-order-status-enum";
import { Card } from "./card";
import { User } from "./user";

export interface Order {
    id: number;
    cardID: number;
    amount: number;
    rechargeDate: Date;
    status: RechargeOrderStatus;
    card: Card | null;
    [key: string]: any;
  }