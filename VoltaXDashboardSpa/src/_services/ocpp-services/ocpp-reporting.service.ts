import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppReportingService {

  baseUrl = environment.apiUrl+"/ocpp/reporting/";

  constructor(
    private _http: HttpClient,
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  getBaseReport(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetBaseReport/"+chargePointID,request, this.httpOptions);
  }
  
  getReport(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetReport/"+chargePointID,request, this.httpOptions);
  }
  
  getMonitoringReport(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetMonitoringReport/"+chargePointID,request, this.httpOptions);
  }
  
  getLog(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetLog/"+chargePointID,request, this.httpOptions);
  }
  
  customerInformation(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"CustomerInformation/"+chargePointID,request, this.httpOptions);
  }
  
}
