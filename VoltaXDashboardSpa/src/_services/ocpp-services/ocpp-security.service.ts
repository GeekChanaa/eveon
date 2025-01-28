import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OcppSecurityService {

  baseUrl = environment.apiUrl+"/ocpp/security/";

  constructor(
    private _http: HttpClient,
  ) { }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  installCertificate(chargePointID : string, request : any){
    return this._http.post<any>(this.baseUrl+"InstallCertificate/"+chargePointID,request, this.httpOptions);
  }
}
