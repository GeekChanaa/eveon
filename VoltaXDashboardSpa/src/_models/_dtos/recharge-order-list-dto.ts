export interface RechargeOrderListDto{
    id : number,
    cardNumber : string,
    amount : number,
    status: string,
    userName: string,
    rechargeDate : Date
  [key: string]: any;
}   