import { Order } from "./order";

export interface User {
    id: number;
    FirstName: string;
    LastName: string;
    Email: string;
    Phone: string;
    Password?: string;
    Orders: Order[];
  }