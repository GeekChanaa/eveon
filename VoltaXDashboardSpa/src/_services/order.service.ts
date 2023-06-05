import { Injectable } from '@angular/core';
import { Order } from 'src/_models/order';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService extends AbstractService<Order>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/order");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/order";

  // Counting orders today
  countOrdersToday(){
    return this._http.get<number>(this.baseUrl+"/countToday");
  }

  // Counting orders By DAy
  countOrdersByDay(){
    return this._http.get<number[]>(this.baseUrl+"/countByDay");
  }

  // Counting orders rechargeAmount total
  countRechargeAmount(){
    return this._http.get<number>(this.baseUrl+"/countRechargeAmount");
  }

  // Counting orders rechargeAmount today
  countRechargeAmountToday(){
    return this._http.get<number>(this.baseUrl+"/countRechargeAmountToday");
  }

  // Counting orders rechargeAmount By day
  countRechargeAmountByDay(){
    return this._http.get<number[]>(this.baseUrl+"/countRechargeAmountByDay");
  }

}
