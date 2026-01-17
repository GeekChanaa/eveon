import { Injectable } from '@angular/core';
import { Order } from 'src/_models/order';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { RechargeOrderDto } from 'src/_models/_dtos/recharge-order-dto';
import { InvoiceDTO } from 'src/_models/_dtos/invoice-dto';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class OrderService extends AbstractService<Order>{


  private isDownloadingSubject = new BehaviorSubject<boolean>(false);
  isDownloading$ = this.isDownloadingSubject.asObservable();

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/order/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/order/";

  getAllRechargeOrders(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetRechargeOrders");
  }

  getOrder(orderID : number){
    return this._http.get<number>(this.baseUrl+"GetOrder/"+orderID);
  }


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

  createRechargeOrder(orderDto : any){
    return this._http.post(this.baseUrl+"CreateRechargeOrder",orderDto);
  }

  // Get invoice info
  getInvoiceInfo(orderID : number) : Observable<InvoiceDTO>{
    return this._http.get<InvoiceDTO>(this.baseUrl+"getInvoiceInfo/"+orderID);
  }

  getCardOrders(page:  number,itemsPerPage : number,itemParams : any,cardID : number){
    return super.getAll(page,itemsPerPage,itemParams,"GetCardOrders/"+cardID);
  }

  getUserOrders(page : number, itemsPerPage: number, itemParams: any, userID : number){
    return super.getAll(page,itemsPerPage,itemParams,"GetUserOrders/"+userID);
  }

  getOrderInvoice(orderID: number): void {
    this.isDownloadingSubject.next(true); // Set to true when the call starts

    this.http
      .get(`${this.baseUrl}GetOrderInvoice/${orderID}`, { responseType: 'blob' })
      .subscribe(
        (data: Blob) => {
          const blob = new Blob([data], { type: 'application/pdf' });
          const url = window.URL.createObjectURL(blob);

          const link = document.createElement('a');
          link.href = url;
          link.download = `OrderInvoice_${orderID}.pdf`;
          link.click();

          window.URL.revokeObjectURL(url);
        },
        (error) => {
          console.error('Error downloading invoice:', error);
        },
        () => {
          this.isDownloadingSubject.next(false); // Set to false when the call completes
        }
      );
  }
}
