import { User } from "./user";

export interface Card {
    id: number;
    cardNumber: string;
    account: string;
    cardType: string;
    expirationDate: Date;
    maxCount: number;
    status: string;
    balance: number;
    note: string;
    userID: number;
    user: User | null;
    [key: string]: any;
}