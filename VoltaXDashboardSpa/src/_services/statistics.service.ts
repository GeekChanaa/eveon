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

  getTotalRevenue() {
    return this._http.get<number>(
      this.baseUrl + "TotalRevenue",
      this.httpOptions
    );
  }

  getTotalRevenueToday() {
    return this._http.get<number>(
      this.baseUrl + "TotalRevenueToday",
      this.httpOptions
    );
  }

  getDailyRevenueLast30Days() {
    return this._http.get<{ [date: string]: number }>(
      this.baseUrl + "DailyRevenueLast30Days",
      this.httpOptions
    );
  }

  getMonthlyRevenueLastYear() {
    return this._http.get<{ [monthYear: string]: number }>(
      this.baseUrl + "MonthlyRevenueLastYear",
      this.httpOptions
    );
  }

}
