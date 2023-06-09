import { City } from "./city";
import { Country } from "./country";

export interface State {
    Id: number;
    Name: string;
    CountryID: number;
    CountryCode: string;
    FipsCode: string;
    Iso2: string;
    Type: string;
    Latitude: number | null;
    Longitude: number | null;
    CreatedAt: Date | null;
    UpdatedAt: Date;
    Flag: boolean;
    WikiDataId: string;
    Country: Country;
    Cities: City[];
}