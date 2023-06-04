import { Injectable } from '@angular/core';
import { CarCharger } from 'src/_models/car-charger';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CarChargerService extends AbstractService<CarCharger>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/carcharger");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"api/carcharger";

}
