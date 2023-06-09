import { Country } from "./country";
import { State } from "./state";

export interface City {
    ID: number;
    Name: string;
    StateID: number | null;
    StateCode: string;
    CountryID: number | null;
    CountryCode: string;
    Latitude: number;
    Longitude: number;
    CreatedAt: Date;
    UpdatedAt: Date;
    Flag: boolean;
    WikiDataId: string;
    State: State;
    Country: Country;
}