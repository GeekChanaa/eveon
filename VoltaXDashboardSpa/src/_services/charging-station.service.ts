import { Injectable } from '@angular/core';
import { ChargingStation } from 'src/_models/charging-station';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';
import { ChargePoint } from 'src/_models/charge-point';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class ChargingStationService extends AbstractService<ChargingStation>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http, snackBar, environment.apiUrl+"/api/chargingstation/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargingstation/";

  // get charging station revenue


  getChargingStationRevenueLast7Days(chargingStationID : number){
    return this._http.get<number[]>(this.baseUrl+"GetChargingStationRevenueLast7Days?chargingStationID="+chargingStationID);
  }

  getChargingStationRevenueLast30Days(chargingStationID : number){
    return this._http.get<number[]>(this.baseUrl+"GetChargingStationRevenueLast30Days?chargingStationID="+chargingStationID);
  }

  getChargingStationRevenueLast12Months(chargingStationID : number){
    return this._http.get<number[]>(this.baseUrl+"GetChargingStationRevenueLast12Months?chargingStationID="+chargingStationID);
  }

  getTop10ChargingStationsByRevenue(){
    return this._http.get<any[]>(this.baseUrl+"GetTop10ChargingStationsByRevenue");
  }

}
