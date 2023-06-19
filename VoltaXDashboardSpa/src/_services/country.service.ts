import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { Country } from 'src/_models/country';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CountryService extends AbstractService<Country>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/country/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/country/";

  // Get All country names
  getAllCountryNames() : Observable<any[]>{
    return this._http.get<any[]>(this.baseUrl+"GetAllCountryNames")
  }


}
