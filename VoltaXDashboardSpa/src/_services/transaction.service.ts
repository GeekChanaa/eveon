import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Transaction } from 'src/_models/transaction';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { Observable } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class TransactionService extends AbstractService<Transaction>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/transaction/");
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

}
