import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RechargeOrderListDto } from 'src/_models/_dtos/recharge-order-list-dto';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-recharge-orders-list',
  templateUrl: './recharge-orders-list.component.html',
  styleUrls: ['./recharge-orders-list.component.sass']
})
export class RechargeOrdersListComponent implements OnInit {

  fields: string[] = [];
  filters : any = {
    type:""
  };
  

  order: RechargeOrderListDto = {
    id: 0,
    cardID: 0,
    amount: 0,
    status: '',
    rechargeDate: new Date()
  }

  // Constructor
  constructor(
    private _orderService: OrderService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getOrdersObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._orderService.getAllRechargeOrders(currentPage, itemsPerPage, itemParams);
  deleteOrderObservable = (id : number) => this._orderService.deleteById(id);
  updateOrderObservable = (id : number, model : any) => this._orderService.edit(id, model);

  private _getItemFields() {
    if (!this.order || this.order == undefined) {
      return;
    }
    Object.keys(this.order ?? {}).forEach((element: string) => {
      if (typeof this.order?.[element] == "object" && this.order?.[element] != null && this.order?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.order?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      type:"",
    }
  }
}
