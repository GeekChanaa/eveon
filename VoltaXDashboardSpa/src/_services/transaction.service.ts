import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Transaction } from 'src/_models/transaction';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { Observable } from 'rxjs';
import { PaginatedResult } from 'src/_models/pagination';

@Injectable({
  providedIn: 'root'
})
export class TransactionService extends AbstractService<Transaction>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/transaction/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/transaction/";

  getTotalEnergyConsumed(){
    return this.http.get<number>(this.baseUrl+"TotalEnergyConsumed");
  }

  getTotalEnergyConsumedToday(){
    return this.http.get<number>(this.baseUrl+"TotalEnergyConsumedToday");
  }

  getDailyEnergyConsumedLast30Days(){
    return this.http.get<any[]>(this.baseUrl+"DailyEnergyConsumedLast30Days");
  }

  getMonthlyEnergyConsumedLastYear(){
    return this.http.get(this.baseUrl+"MonthlyEnergyConsumedLastYear");
  }

  getTotalEnergyConsumedBetween(dateStart : Date, dateEnd : Date){
    return this.http.get<number>(this.baseUrl+"GetTotalEnergyConsumedBetween?dateStart="+dateStart+"&dateEnd="+dateEnd);
  }

  // getting latest transactions
  getLatestTransactions(){
    return this.http.get<any[]>(this.baseUrl+"GetLatestTransactions");
  }

  getChargePointTransactions(chargePointID : number,page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<any[]>>{
    return super.getAll(page,itemsPerPage,itemParams,"GetChargePointTransactions/"+chargePointID);
  }



  // Partner functions

  partnerTotalEnergyConsumed(partnerID : number){
    return this._http.get<number>(this.baseUrl+"PartnerTotalEnergyConsumed/"+partnerID);
  }

  partnerTotalEnergyConsumedToday(partnerID : number){
    return this._http.get(this.baseUrl+"PartnerTotalEnergyConsumedToday/"+partnerID);
  }

  partnerDailyEnergyConsumedLast30Days(partnerID : number){
    return this._http.get(this.baseUrl+"PartnerDailyEnergyConsumedLast30Days/"+partnerID);
  }

  partnerMonthlyEnergyConsumedLastYear(partnerID : number){
    return this._http.get(this.baseUrl+"PartnerMonthlyEnergyConsumedLastYear/"+partnerID);
  }

  getPartnerTotalEnergyConsumedBetween(partnerID : number, dateStart : Date, dateEnd : Date){
    return this._http.get<number>(this.baseUrl+"GetPartnerTotalEnergyConsumedBetween/"+partnerID+"?dateStart="+dateStart+"&dateEnd="+dateEnd);
  }

  getPartnerLatestTransactions(partnerID : number){
    return this._http.get<any[]>(this.baseUrl+"GetPartnerLatestTransactions/"+partnerID);
  }

  getCardTransactions(page:  number,itemsPerPage : number,itemParams : any,cardID : number){
    return super.getAll(page,itemsPerPage,itemParams,"GetCardTransactions/"+cardID);
  }


}
