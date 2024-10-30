import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppConfigurationService {

  baseUrl = environment.apiUrl+"/ocpp/configuration/";

  constructor(
    private _http: HttpClient,
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };
  
  setNetworkProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetNetworkProfile/"+chargePointID,request, this.httpOptions);
  }

  
  clearDisplayMessage(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearDisplayMessage/"+chargePointID,request, this.httpOptions);
  }
  
  getDisplayMessages(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetDisplayMessages/"+chargePointID,request, this.httpOptions);
  }
  
  publishFirmware(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"PublishFirmware/"+chargePointID,request, this.httpOptions);
  }
  
  setDisplayMessage(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetDisplayMessage/"+chargePointID,request, this.httpOptions);
  }
  
  unpublishFirmware(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"UnpublishFirmware/"+chargePointID,request, this.httpOptions);
  }
  
  updateFirmware(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"UpdateFirmware/"+chargePointID,request, this.httpOptions);
  }
  
  reset(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"Reset/"+chargePointID,request, this.httpOptions);
  }
  
  changeAvailability(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ChangeAvailability/"+chargePointID,request, this.httpOptions);
  }
  
  triggerMessage(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"TriggerMessage/"+chargePointID,request, this.httpOptions);
  }

}
