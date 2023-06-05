import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Transaction } from 'src/_models/transaction';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TransactionService extends AbstractService<Transaction>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/transaction");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/transaction";

  // count total energy
  countEnergy() : Observable<number>{
    return this._http.get<number>(this.baseUrl+"/countEnergy");
  }

  // count energy today
  countEnergyToday() : Observable<number>{
    console.log(this.baseUrl+"/countEnergyToday")
    return this._http.get<number>(this.baseUrl+"/countEnergyToday");
  }

  // count energy by day
  countEnergyByDay() : Observable<number[]>{
    return this._http.get<number[]>(this.baseUrl+"/countEnergyByDay");
  }
}
