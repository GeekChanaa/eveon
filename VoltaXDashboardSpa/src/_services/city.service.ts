import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { City } from 'src/_models/city';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class CityService extends AbstractService<City>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/City/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/City/";

  // Get all cities by state
  getCitiesByStateID(stateID : number){
    return this._http.get<any[]>(this.baseUrl+"GetAllCityNamesByState?stateID="+stateID);
  }

  // Get All morocco cities
  getAllMoroccoCityNames(){
    return this._http.get<any[]>(this.baseUrl+"GetAllMoroccoCityNames");
  }
}
