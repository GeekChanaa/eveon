import { City } from "./city";
import { State } from "./state";

export interface Country {
    Id: number;
    Name: string;
    Iso3: string;
    NumericCode: string;
    Iso2: string;
    Phonecode: string;
    Capital: string;
    Currency: string;
    CurrencyName: string;
    CurrencySymbol: string;
    Tld: string;
    Native: string;
    Region: string;
    Subregion: string;
    Timezones: string;
    Translations: string;
    Latitude: number | null;
    Longitude: number | null;
    Emoji: string;
    EmojiU: string;
    CreatedAt: Date | null;
    UpdatedAt: Date;
    Flag: boolean;
    WikiDataId: string;
    States: State[];
    Cities: City[];
}