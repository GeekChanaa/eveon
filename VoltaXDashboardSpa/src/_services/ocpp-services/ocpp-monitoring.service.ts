import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppMonitoringService {

  baseUrl = environment.apiUrl+"/ocpp/monitoring/";

  constructor(
    private _http: HttpClient,
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  setVariableMonitoring(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetVariableMonitoring/"+chargePointID,request, this.httpOptions);
  }

  clearVariableMonitoring(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearVariableMonitoring/"+chargePointID,request, this.httpOptions);
  }

  setMonitoringLevel(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetMonitoringLevel/"+chargePointID,request, this.httpOptions);
  }

  setMonitoringBase(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetMonitoringBase/"+chargePointID,request, this.httpOptions);
  }

  setVariables(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetVariables/"+chargePointID,request, this.httpOptions);
  }

  getVariables(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetVariables/"+chargePointID,request, this.httpOptions);
  }
}
