import {User} from '../user';

export interface CardDto {
    id: number;
    cardNumber: string;
    name: string;
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