import { CardStatusEnum } from "./_enums/card-status";
import { User } from "./user";

export interface Card {
    id: number;
    cardNumber: string;
    cardType: string;
    expirationDate: Date;
    maxCount: number;
    status: CardStatusEnum;
    balance: number;
    note: string;
    userID: number;
    user: User | null;
    [key: string]: any;
}