import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PartnerConnectorStatusService {

    constructor(private _http : HttpClient) {
      }
    
    // Base URL for the api
    getBaseUrl = (partnerID : number) => environment.apiUrl+"/api/partners/"+partnerID+"/PartnerConnectorStatus/";


    // get card transactions 
    getNumberOfPartnerConnectorsByAllStatus(partnerID : number){
      return this._http.get<any>(this.getBaseUrl(partnerID)+"getNumberOfPartnerConnectorsByAllStatus");
    }
}
