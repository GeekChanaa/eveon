import { HttpClient } from '@angular/common/http';
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

  
  clearChargingProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearChargingProfile/"+chargePointID,request);
  }
  
  getChargingProfiles(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetChargingProfiles/"+chargePointID,request);
  }
  
  setChargingProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetChargingProfile/"+chargePointID,request);
  }
  
  clearedChargingLimit(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearedChargingLimit/"+chargePointID,request);
  }
  
  getCompositeSchedule(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetCompositeSchedule/"+chargePointID,request);
  }

}
