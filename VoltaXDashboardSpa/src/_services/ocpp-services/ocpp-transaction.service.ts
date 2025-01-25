import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppTransactionService {

  baseUrl = environment.apiUrl+"/ocpp/transactions/";

  constructor(
    private _http: HttpClient,
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };
  
  clearChargingProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearChargingProfile/"+chargePointID,request, this.httpOptions);
  }
  
  getTransactionStatus(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetTransactionStatus/"+chargePointID,request, this.httpOptions);
  }
  
}
