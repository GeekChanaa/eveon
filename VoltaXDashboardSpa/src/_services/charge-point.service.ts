import { Injectable } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChargePointService extends AbstractService<ChargePoint>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargepoint");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargepoint";

}
