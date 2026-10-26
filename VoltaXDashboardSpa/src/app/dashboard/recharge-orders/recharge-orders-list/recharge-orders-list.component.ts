import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
import { RechargeOrderListDto } from 'src/_models/_dtos/recharge-order-list-dto';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-recharge-orders-list',
  templateUrl: './recharge-orders-list.component.html',
  styleUrls: ['./recharge-orders-list.component.sass']
})
export class RechargeOrdersListComponent implements OnInit {

  fields: string[] = [];
  filterDefs: ListFilterDef[] = [
    { key: 'status', label: 'Status', type: 'multiselect', options: [{ value: 'Pending', label: 'Pending' }, { value: 'Processing', label: 'Processing' }, { value: 'Completed', label: 'Completed' }, { value: 'Failed', label: 'Failed' }, { value: 'Canceled', label: 'Canceled' }, { value: 'Refunded', label: 'Refunded' }] },
    { key: 'provider', label: 'Payment provider', type: 'multiselect', options: [{ value: 'Manual', label: 'Manual' }, { value: 'Cmi', label: 'Cmi' }, { value: 'Stripe', label: 'Stripe' }, { value: 'Sandbox', label: 'Sandbox' }] },
    { key: 'amount', label: 'Amount', type: 'number-range' },
    { key: 'rechargeDate', label: 'Recharge date', type: 'date-range' },
    { key: 'paidAt', label: 'Paid on', type: 'date-range' },
  ];
  

  order: RechargeOrderListDto = {
    id: 0,
    cardNumber: "",
    amount: 0,
    status: '',
    userName: '',
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
}
