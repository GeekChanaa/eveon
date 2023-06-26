import { Order } from "./order";

export interface User {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    password?: string;
    orders?: Order[];
    role : string;
    [key: string]: any;
  }