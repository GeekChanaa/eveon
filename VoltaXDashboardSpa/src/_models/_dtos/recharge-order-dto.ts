export interface RechargeOrderDto {
    CardID: number;
    RechargeAmount: number;
    DebitCardID?: number;
    UserID: number;
    SaveCard: boolean;
    MockPaymentStatus?: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Canceled' | 'Refunded';
}
