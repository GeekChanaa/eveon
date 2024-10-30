import { HttpClient, HttpHeaders } from '@angular/common/http';
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

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  
  requestStartTransaction(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"RequestStartTransaction/"+chargePointID,request, this.httpOptions);
  }
  
  requestStopTransaction(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"RequestStopTransaction/"+chargePointID,request, this.httpOptions);
  }
  
  cancelReservation(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"CancelReservation/"+chargePointID,request, this.httpOptions);
  }
  
  reserveNow(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ReserveNow/"+chargePointID,request, this.httpOptions);
  }
  
  unlockConnector(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"UnlockConnector/"+chargePointID,request, this.httpOptions);
  }
  
  clearCache(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearCache/"+chargePointID,request, this.httpOptions);
  }
  
  sendLocalList(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SendLocalList/"+chargePointID,request, this.httpOptions);
  }
  
  getLocalListVersion(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetLocalListVersion/"+chargePointID,request, this.httpOptions);
  }
}
