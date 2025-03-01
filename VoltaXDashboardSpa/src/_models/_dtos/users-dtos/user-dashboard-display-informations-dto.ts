export interface UserDashboardDisplayInformationsDto{
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    birthday? : string;
    gender? : string;
    isEmailVerified : Boolean;
    isPhoneVerified : Boolean;
    role : string;
    [key: string]: any;
}