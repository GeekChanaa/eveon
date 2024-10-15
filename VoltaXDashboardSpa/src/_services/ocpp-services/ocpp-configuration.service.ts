import { HttpClient } from '@angular/common/http';
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

  
  setNetworkProfile(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetNetworkProfile/"+chargePointID,request);
  }

  
  clearDisplayMessage(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ClearDisplayMessage/"+chargePointID,request);
  }
  
  getDisplayMessages(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"GetDisplayMessages/"+chargePointID,request);
  }
  
  publishFirmware(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"PublishFirmware/"+chargePointID,request);
  }
  
  setDisplayMessage(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"SetDisplayMessage/"+chargePointID,request);
  }
  
  unpublishFirmware(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"UnpublishFirmware/"+chargePointID,request);
  }
  
  updateFirmware(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"UpdateFirmware/"+chargePointID,request);
  }
  
  reset(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"Reset/"+chargePointID,request);
  }
  
  changeAvailability(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"ChangeAvailability/"+chargePointID,request);
  }
  
  triggerMessage(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"TriggerMessage/"+chargePointID,request);
  }

}
