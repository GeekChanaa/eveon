import { HttpClient } from '@angular/common/http';
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

  getBaseReport(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetBaseReport/"+chargePointID,request);
  }
  
  getReport(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetReport/"+chargePointID,request);
  }
  
  getMonitoringReport(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetMonitoringReport/"+chargePointID,request);
  }
  
  getLog(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetLog/"+chargePointID,request);
  }
  
  customerInformation(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"CustomerInformation/"+chargePointID,request);
  }
  
}
