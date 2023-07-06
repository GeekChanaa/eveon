import { UserRole } from "./_enums/user-role";
import { Order } from "./order";

export interface User {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    isEmailVerified : Boolean;
    isPhoneVerified : Boolean;
    password?: string;
    orders?: Order[];
    role : UserRole;
    [key: string]: any;
  }