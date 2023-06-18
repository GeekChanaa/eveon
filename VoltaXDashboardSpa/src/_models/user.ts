import { Order } from "./order";

export interface User {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    password?: string;
    orders?: Order[];
    [key: string]: any;
  }