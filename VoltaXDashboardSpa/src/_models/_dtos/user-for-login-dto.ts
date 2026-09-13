export interface UserForLoginDto{
    // Either one identifies the account, the API takes whichever is filled in.
    email? : string,
    phone? : string,
    password:  string
}
