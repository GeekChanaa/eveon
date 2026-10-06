export interface DebitCardListingDto {
    id: number;
    userID: number;
    type: 'Visa' | 'Mastercard' | 'Generic';
    last4: string;
    expiryMonth: number;
    expiryYear: number;
    cardNumberHidden: string;
    nameHidden: string;
  }
