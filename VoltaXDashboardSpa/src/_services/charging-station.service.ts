import { Injectable } from '@angular/core';
import { ChargingStation } from 'src/_models/charging-station';
import { AbstractService } from './abstract-service';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';
import { ChargePoint } from 'src/_models/charge-point';

import { PaginatedResult } from 'src/_models/pagination';
import { ChargingStationCreateDto } from 'src/_models/_dtos/charging-station-create-dto';

@Injectable({
  providedIn: 'root'
})
export class ChargingStationService extends AbstractService<ChargingStation>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargingstation/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargingstation/";

  // get charging station revenue


  getChargingStationRevenueLast7Days(chargingStationID : number){
    return this._http.get<number[]>(this.baseUrl+"GetChargingStationRevenueLast7Days?chargingStationID="+chargingStationID);
  }

  getChargingStationRevenueLast30Days(chargingStationID : number){
    return this._http.get<number[]>(this.baseUrl+"GetChargingStationRevenueLast30Days?chargingStationID="+chargingStationID);
  }

  getChargingStationRevenueLast12Months(chargingStationID : number){
    return this._http.get<number[]>(this.baseUrl+"GetChargingStationRevenueLast12Months?chargingStationID="+chargingStationID);
  }

  getTop10ChargingStationsByRevenue(){
    return this._http.get<any[]>(this.baseUrl+"GetTop10ChargingStationsByRevenue");
  }

  // PARTNER CHARGING STATIONS
  getPartnerChargingStations(partnerID : number,page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<ChargingStation[]>>{
    return super.getAll(page,itemsPerPage,itemParams,"GetPartnerChargingStations/"+partnerID);
  }

  getPartnerChargingStationRevenue(partnerID : number){
    return this._http.get(this.baseUrl+"GetPartnerChargingStationRevenue/"+partnerID);
  }

  getPartnerChargingStationRevenueLast7Days(partnerID : number){
    return this._http.get(this.baseUrl+"GetPartnerChargingStationRevenueLast7Days/"+partnerID);
  }

  getPartnerChargingStationRevenueLast30Days(partnerID : number){
    return this._http.get(this.baseUrl+"GetPartnerChargingStationRevenueLast30Days/"+partnerID);
  }

  getPartnerChargingStationRevenueLast12Months(partnerID : number){
    return this._http.get(this.baseUrl+"GetPartnerChargingStationRevenueLast12Months/"+partnerID);
  }

  getPartnerTop10ChargingStationsByRevenue(partnerID : number){
    return this._http.get<any[]>(this.baseUrl+"GetPartnerTop10ChargingStationsByRevenue/"+partnerID);
  }

  getChargingStationByID(chargingStationID : number){
    return this._http.get<any>(this.baseUrl+"GetChargingStationForDisplay/"+chargingStationID);
  }

  createChargingStation(chargingStationCreateDto : FormData){
    return this._http.post<ChargingStationCreateDto>(this.baseUrl+"Add", chargingStationCreateDto);
  }

  getChargingStationNames(){
    return this._http.get<any[]>(this.baseUrl+"GetChargingStationNames");
  }

  chargingStationExistsByName(name : string){
    return this._http.get<boolean>(this.baseUrl+"ChargingStationExistsByName/"+name);
  }

  


}
