import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { OcppDefaultVariable } from 'src/_models/ocpp-provisioning';

@Injectable({
  providedIn: 'root'
})
export class OcppDefaultConfigurationService {

  baseUrl = environment.apiUrl + "/api/OcppDefaultConfiguration/";

  constructor(private _http: HttpClient) { }

  getProfile() {
    return this._http.get<OcppDefaultVariable[]>(this.baseUrl + "GetProfile");
  }

  saveProfile(profile: OcppDefaultVariable[]) {
    return this._http.put<OcppDefaultVariable[]>(this.baseUrl + "SaveProfile", profile);
  }

  resetProfile() {
    return this._http.post<OcppDefaultVariable[]>(this.baseUrl + "ResetProfile", {});
  }
}
