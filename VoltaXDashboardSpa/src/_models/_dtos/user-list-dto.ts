export interface UserListDto{
    id : number,
    firstName : string,
    lastName : string,
    email : string,
    gender : string,
    city : string,
    car : string,
    birthday : Date,
    phone : string,
    partnerName : string,
    isEmailVerified : boolean,
    isPhoneNumberVerified : boolean
    role : string
    suspendedAt : string
    fullName : string
    [key: string]: any;
}