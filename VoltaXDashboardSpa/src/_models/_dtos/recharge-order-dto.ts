export interface RechargeOrderDto {
    CardID: number;
    RechargeAmount: number;
    CardNumber?: string;
    CardHolderName?: string;
    CardExpirationDate?: string;
    CardCVV?: string;
    DebitCardID?: number;
    UserID: number;
    SaveCard: boolean;
}
