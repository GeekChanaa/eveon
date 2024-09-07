import { Injectable } from '@angular/core';
import { ChargePoint } from 'src/_models/charge-point';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';

import { PaginatedResult } from 'src/_models/pagination';
@Injectable({
  providedIn: 'root'
})
export class ChargePointService extends AbstractService<ChargePoint>{

  constructor(
    protected http : HttpClient
    ) {
    super(http,environment.apiUrl+"/api/chargepoint/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargepoint/";


  override create(model: any): Observable<ChargePoint> {
    model.chargePointID = this.generateChargePointID();
    return this._http.post<ChargePoint>(this.actionUrl, model, this.httpOptions).pipe(map(response => {
      return response;
    }));
  }


  generateChargePointID(): string {
    const alphaNumChars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    const length = alphaNumChars.length;
    let result = 'VX'; // Start with 'VX'

    // Generate the 'VVVVVVVV' part
    for (let i = 0; i < 8; i++) {
      const randomIndex = Math.floor(Math.random() * length);
      result += alphaNumChars[randomIndex];
    }

    return result;
  }

  getPartnerChargePoints(partnerID : number,page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<ChargePoint[]>>{
    return super.getAll(page,itemsPerPage,itemParams,"GetPartnerChargePoints/"+partnerID);
  }

  getChargingStationChargePoints(id : number){
    return this._http.get<any[]>(this.baseUrl+"GetChargingStationChargePoints/"+id);
  }
}
