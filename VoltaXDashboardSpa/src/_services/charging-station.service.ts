import { Injectable } from '@angular/core';
import { ChargingStation } from 'src/_models/charging-station';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';
import { ChargePoint } from 'src/_models/charge-point';

@Injectable({
  providedIn: 'root'
})
export class ChargingStationService extends AbstractService<ChargingStation>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargingstation/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargingstation/";
}
