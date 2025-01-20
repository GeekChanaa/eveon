import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ChargePointStatisticsSummaryDto } from 'src/_models/_dtos/statistics-dtos/charge-point-summary-statistics-dto';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StatisticsService {

  baseUrl = environment.apiUrl+"/api/Statistics/";

  constructor(
    protected _http: HttpClient) {
  }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };

  getChargePointStatisticsSummary(chargePointID : number){
    return this._http.get<ChargePointStatisticsSummaryDto>(this.baseUrl+"GetChargePointStatisticsSummary/"+chargePointID,this.httpOptions);
  }

}
