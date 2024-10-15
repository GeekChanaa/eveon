import { HttpClient } from '@angular/common/http';
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

  setVariableMonitoring(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetVariableMonitoring/"+chargePointID,request);
  }

  clearVariableMonitoring(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearVariableMonitoring/"+chargePointID,request);
  }

  setMonitoringLevel(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetMonitoringLevel/"+chargePointID,request);
  }

  setMonitoringBase(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetMonitoringBase/"+chargePointID,request);
  }

  setVariables(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetVariables/"+chargePointID,request);
  }

  getVariables(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetVariables/"+chargePointID,request);
  }
}
