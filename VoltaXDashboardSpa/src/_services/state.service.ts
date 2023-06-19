import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { State } from 'src/_models/state';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StateService extends AbstractService<State>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/State/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/State/";

  // Gett all state names by country id
  getStatesByCountryID(countryID : number){
    return this._http.get<any[]>(this.baseUrl+"GetAllStateNamesByCountry?countryID="+countryID);
  }

}
