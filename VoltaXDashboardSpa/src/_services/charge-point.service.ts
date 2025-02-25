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

  isChargePointIDUnique(chargePointID : string){
    return this._http.get<boolean>(this.baseUrl+"IsChargePointIDUnique/"+chargePointID);
  }

  getChargePointByID(id : number){
    return this._http.get<any[]>(this.baseUrl+"getChargePointByID/"+id);
  }

  isChargePointSerialNumberUnique(chargePointSerialNumber : string){
    return this._http.get<boolean>(this.baseUrl+"isChargePointSerialNumberUnique/"+chargePointSerialNumber);
  }

  getAllChargePoints(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetAllChargePoints");
  }

  
  getChargePointIds(){
    return this._http.get<any[]>(this.baseUrl+"GetChargePointsIds/").pipe(
      map(chargePoints => chargePoints.map(chargePoint => ({
        id: chargePoint.id,
        name: chargePoint.chargePointID
      })))
    );
  }

  setShowOnMap(chargePointID : number,val : boolean){
    return this._http.put<any>(this.baseUrl+"setShowOnMap/"+chargePointID,val)
  }

  setHasChargeCable(chargePointID : number,val : boolean){
    return this._http.put<any>(this.baseUrl+"setHasChargeCable/"+chargePointID,val)
  }

  

}
