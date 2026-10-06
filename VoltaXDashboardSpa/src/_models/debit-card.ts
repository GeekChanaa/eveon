export type DebitCardBrand = 'Visa' | 'Mastercard' | 'Generic';

/** A saved card: display metadata only. The number and CVV stay with the payment provider. */
export interface DebitCard{
    id : number;
    userID : number;
    name? : string;
    brand : DebitCardBrand;
    last4 : string;
    expiryMonth : number;
    expiryYear : number;
}

/** Body of POST api/debitCard: a provider token plus display metadata, never a card number. */
export interface AddDebitCardRequest {
    providerToken : string;
    brand : DebitCardBrand;
    last4 : string;
    expiryMonth : number;
    expiryYear : number;
    name? : string;
}
