export interface RechargeOrderListDto{
    id : number,
    cardID : number,
    amount : number,
    status: string,
    rechargeDate : Date
  [key: string]: any;
}   