import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { City } from 'src/_models/city';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';

@Injectable({
  providedIn: 'root'
})
export class CityService extends AbstractService<City>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/City");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/City";


}
