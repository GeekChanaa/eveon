import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppSmartChargingService {

  baseUrl = environment.apiUrl+"/ocpp/smartCharging/";

  constructor(
    private _http: HttpClient,
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };
  
  clearChargingProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearChargingProfile/"+chargePointID,request, this.httpOptions);
  }
  
  getChargingProfiles(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetChargingProfiles/"+chargePointID,request, this.httpOptions);
  }
  
  setChargingProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetChargingProfile/"+chargePointID,request, this.httpOptions);
  }
  
  getCompositeSchedule(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetCompositeSchedule/"+chargePointID,request, this.httpOptions);
  }

}
