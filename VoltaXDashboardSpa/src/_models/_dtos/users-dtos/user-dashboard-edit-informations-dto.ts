export interface UserDashboardEditInformationsDto{
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    electricVehicleModelID: string;
    phone: string;
    birthday? : string;
    gender? : string;
    isEmailVerified : Boolean;
    isPhoneVerified : Boolean;
    role : string;
    [key: string]: any;
}