import { CardStatusEnum } from "../_enums/card-status";
import { CardTypeEnum } from "../_enums/card-type";

export interface CardListDto {
    cardNumber: string;
    cardType: CardTypeEnum;
    expirationDate: string; 
    maxCount: number;
    status: CardStatusEnum;
    balance: number;
    note: string;
    userId?: number;
    userName: string;
    [key: string]: any;
  }
  