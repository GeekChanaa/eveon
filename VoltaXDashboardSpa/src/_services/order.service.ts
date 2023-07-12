import { Injectable } from '@angular/core';
import { Order } from 'src/_models/order';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RechargeOrderDto } from 'src/_models/_dtos/recharge-order-dto';
import { InvoiceDTO } from 'src/_models/_dtos/invoice-dto';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OrderService extends AbstractService<Order>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/order/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/order/";

  // Counting orders today
  countOrdersToday(){
    return this._http.get<number>(this.baseUrl+"countToday");
  }

  // Counting orders By DAy for the last 30 days
  countOrdersByDay(){
    return this._http.get<number[]>(this.baseUrl+"countByDay");
  }

  // counting orders by day for the last 7 days
  countbylast7Days(){
    return this._http.get<number[]>(this.baseUrl+"countbylast7Days");
  }

  // counting orders by day for the last 12 months by month
  countbylast12months(){
    return this._http.get<number[]>(this.baseUrl+"countbylast12months");
  }


  // Counting orders rechargeAmount total
  countRechargeAmount(){
    return this._http.get<number>(this.baseUrl+"countRechargeAmount");
  }

  // Counting orders rechargeAmount today
  countRechargeAmountToday(){
    return this._http.get<number>(this.baseUrl+"countRechargeAmountToday");
  }

  // Counting orders rechargeAmount By day
  countRechargeAmountByDay(){
    return this._http.get<number[]>(this.baseUrl+"countRechargeAmountByDay");
  }

  // Counting order recharge amount between 2 dates
  countRechargeAmountBetween(dateStart : Date, dateEnd : Date){
    return this._http.get<number>(this.baseUrl+"countRechargeAmountBetween?dateStart="+dateStart+"&dateEnd="+dateEnd);
  }

  // recharge card order
  rechargeCard(orderDto : RechargeOrderDto){
    return this._http.post(this.baseUrl+"RechargeOrder",orderDto);
  }

  // Get invoice info
  getInvoiceInfo(orderID : number) : Observable<InvoiceDTO>{
    return this._http.get<InvoiceDTO>(this.baseUrl+"getInvoiceInfo/"+orderID);
  }

}
