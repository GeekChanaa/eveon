import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChargePointStatisticsSummaryDto } from 'src/_models/_dtos/statistics-dtos/charge-point-summary-statistics-dto';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PartnerStatisticsService {

  baseUrl = environment.apiUrl + "/api/partner/statistics/";

  constructor(
    protected _http: HttpClient) {
  }

  httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json; charset=utf-8' })
  };




  getTotalRevenue(partnerID: number) {
    return this._http.get<number>(
      this.baseUrl + "TotalRevenue/" + partnerID,
      this.httpOptions
    );
  }

  getTotalRevenueToday(partnerID: number) {
    return this._http.get<number>(
      this.baseUrl + "TotalRevenueToday/" + partnerID,
      this.httpOptions
    );
  }

  getDailyRevenueLast30Days(partnerID: number) {
    return this._http.get<{ [date: string]: number }>(
      this.baseUrl + "DailyRevenueLast30Days/" + partnerID,
      this.httpOptions
    );
  }

  getMonthlyRevenueLastYear(partnerID: number) {
    return this._http.get<{ [monthYear: string]: number }>(
      this.baseUrl + "MonthlyRevenueLastYear/" + partnerID,
      this.httpOptions
    );
  }

  // Charged Minutes endpoints
  getTotalChargedMinutes(partnerID: number) {
    return this._http.get<number>(
      this.baseUrl + "TotalChargedMinutes/" + partnerID,
      this.httpOptions
    );
  }

  getTotalChargedMinutesToday(partnerID: number) {
    return this._http.get<number>(
      this.baseUrl + "TotalChargedMinutesToday/" + partnerID,
      this.httpOptions
    );
  }

  getDailyChargedMinutesLast30Days(partnerID: number) {
    return this._http.get<{ [date: string]: number }>(
      this.baseUrl + "DailyChargedMinutesLast30Days/" + partnerID,
      this.httpOptions
    );
  }

  getMonthlyChargedMinutesLastYear(partnerID: number) {
    return this._http.get<{ [monthYear: string]: number }>(
      this.baseUrl + "MonthlyChargedMinutesLastYear/" + partnerID,
      this.httpOptions
    );
  }

  // Energy Consumed endpoints
  getTotalEnergyConsumed(partnerID: number) {
    return this._http.get<number>(
      this.baseUrl + "TotalEnergyConsumed/" + partnerID,
      this.httpOptions
    );
  }

  getTotalEnergyConsumedToday(partnerID: number) {
    return this._http.get<number>(
      this.baseUrl + "TotalEnergyConsumedToday/" + partnerID,
      this.httpOptions
    );
  }

  getDailyEnergyConsumedLast30Days(partnerID: number) {
    return this._http.get<{ [date: string]: number }>(
      this.baseUrl + "DailyEnergyConsumedLast30Days/" + partnerID,
      this.httpOptions
    );
  }

  getMonthlyEnergyConsumedLastYear(partnerID: number) {
    return this._http.get<{ [monthYear: string]: number }>(
      this.baseUrl + "MonthlyEnergyConsumedLastYear/" + partnerID,
      this.httpOptions
    );
  }

  // Get the total charging sessions for a specific partner
  getTotalChargingSessions(partnerID: number): Observable<number> {
    return this._http.get<number>(
      this.baseUrl + "TotalChargingSessions/" + partnerID
    );
  }

  // Get the total charging sessions for a specific partner today
  getTotalChargingSessionsToday(partnerID: number): Observable<number> {
    return this._http.get<number>(
      this.baseUrl + "TotalChargingSessionsToday/" + partnerID
    );
  }

  // Get daily charging sessions for the last 30 days for a specific partner
  getDailyChargingSessionsLast30Days(partnerID: number): Observable<{ [date: string]: number }> {
    return this._http.get<{ [date: string]: number }>(
      this.baseUrl + "DailyChargingSessionsLast30Days/" + partnerID
    );
  }




}
