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

  refreshConnectors(chargePointID : string){
    return this._http.post<any>(this.baseUrl+"RefreshConnectors/"+chargePointID,null, this.httpOptions);
  }

  /** OCPP version negotiated by the charger (null when it is not connected). */
  getProtocolVersion(chargePointID : string){
    return this._http.get<ProtocolVersionResult>(this.baseUrl+"ProtocolVersion/"+encodeURIComponent(chargePointID), this.httpOptions);
  }

  /** OCPP 1.6 GetConfiguration. Empty keys = every key. */
  getConfiguration(chargePointID : string, keys : string[] = []){
    return this._http.post<OcppCommandResult<GetConfigurationResponse>>(this.baseUrl+"GetConfiguration/"+chargePointID, { keys }, this.httpOptions);
  }

  /** OCPP 1.6 ChangeConfiguration of one key. */
  changeConfiguration(chargePointID : string, key : string, value : string){
    return this._http.post<OcppCommandResult<any>>(this.baseUrl+"ChangeConfiguration/"+chargePointID, { key, value }, this.httpOptions);
  }

}

export type OcppProtocolVersion = 'ocpp1.6' | 'ocpp2.0.1';

export interface ProtocolVersionResult {
  chargePointId : string;
  protocolVersion : OcppProtocolVersion | string | null;
  isOnline : boolean;
}

export interface OcppCommandResult<T> {
  message : string;
  status : string | null;
  response : T;
}

export interface Ocpp16ConfigurationKey {
  key : string;
  readonly : boolean;
  value : string | null;
}

export interface GetConfigurationResponse {
  configurationKey : Ocpp16ConfigurationKey[] | null;
  unknownKey : string[] | null;
}

/** "ocpp1.6" -> "OCPP 1.6"; null -> null. */
export function ocppVersionLabel(protocolVersion : string | null | undefined) : string | null {
  if (!protocolVersion) return null;
  return protocolVersion.toLowerCase().startsWith('ocpp') ? 'OCPP ' + protocolVersion.substring(4) : protocolVersion;
}
