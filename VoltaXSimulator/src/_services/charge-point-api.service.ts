import { Injectable } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { ChargePointApi } from 'src/_models/charge-point-api';

@Injectable({
  providedIn: 'root'
})
export class ChargePointApiService extends AbstractService<ChargePointApi>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargepoint/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargepoint/";

  // Connectors
  getChargePointConnectors(chargePointID : number){
    return this.http.get<any[]>(this.baseUrl+"GetChargePointConnectors?chargePointID="+chargePointID);
  }

}
