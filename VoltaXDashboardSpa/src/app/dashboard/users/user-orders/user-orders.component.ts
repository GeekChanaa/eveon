import { Component, Input, OnInit } from '@angular/core';
import { RechargeOrderListDto } from 'src/_models/_dtos/recharge-order-list-dto';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-user-orders',
  templateUrl: './user-orders.component.html',
  styleUrls: ['./user-orders.component.sass']
})
export class UserOrdersComponent implements OnInit {

  @Input() userID : number = 0;
  
  constructor(
    private _orderService : OrderService
  ) { }

  fields: string[] = [];
  filters : any = {
    role:""
  };
  

  order: RechargeOrderListDto = {
    id: 0,
    cardID: 0,
    amount: 0,
    status: '',
    rechargeDate: new Date()
  }


  ngOnInit() {
    this._getItemFields();
    console.log("this is the user ID : " + this.userID);
  }

  getUserOrdersObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._orderService.getUserOrders(currentPage ?? 0, itemsPerPage ?? -1, itemParams, this.userID);

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
      role:""
    }
  }

}
