import { Customer } from "./customer";

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
    customerID: number;
    customer: Customer | null;
    [key: string]: any;
}