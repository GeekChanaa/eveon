import { Customer } from "./customer";

export interface Card {
    id: number;
    CardNumber: string;
    Account: string;
    CardType: string;
    ExpirationDate: Date;
    MaxCount: number;
    Status: string;
    Balance: number;
    Note: string;
    CustomerID: number;
    Customer: Customer;
}