import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppEvDriverService {

  baseUrl = environment.apiUrl+"/ocpp/EVDriver/";

  constructor(
    private _http: HttpClient,
  ) { }

  
  requestStartTransaction(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"RequestStartTransaction/"+chargePointID,request);
  }
  
  requestStopTransaction(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"RequestStopTransaction/"+chargePointID,request);
  }
  
  cancelReservation(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"CancelReservation/"+chargePointID,request);
  }
  
  reserveNow(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ReserveNow/"+chargePointID,request);
  }
  
  unlockConnector(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"UnlockConnector/"+chargePointID,request);
  }
  
  clearCache(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearCache/"+chargePointID,request);
  }
  
  sendLocalList(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SendLocalList/"+chargePointID,request);
  }
  
  getLocalListVersion(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetLocalListVersion/"+chargePointID,request);
  }
}
