import { User } from "./user";

export interface Customer {
    id: number;
    UserID: number;
    Sold: boolean;
    User: User;
}