import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ApiHelper } from 'src/_helpers/api-helpers';
import { ChargePoint } from 'src/_models/charge-point';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PartnerChargePointsService {
   
  constructor(
    private _http : HttpClient,
    private _apiHelper : ApiHelper<ChargePoint>) {
      this._apiHelper = new ApiHelper<ChargePoint>(this._http);
  }
    
  // Base URL for the api
  getBaseUrl = (partnerID : number) => environment.apiUrl+"/api/partners/"+partnerID+"/PartnerChargePoint/";


  getAllPartnerChargePoints(partnerID : number , page?: number, itemsPerPage?: number, itemParams?: any){
    return this._apiHelper.getAll(this.getBaseUrl(partnerID),"GetAllPartnerChargePoints",page,itemsPerPage,itemParams);
  }


}
