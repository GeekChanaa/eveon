import { Component, Input, OnInit } from '@angular/core';
import { Order } from 'src/_models/order';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-charging-card-orders',
  templateUrl: './charging-card-orders.component.html',
  styleUrls: ['./charging-card-orders.component.sass']
})
export class ChargingCardOrdersComponent implements OnInit {
  @Input() cardID : number = 0;
  fields : any[] = [];

  order : Order = {
    id: 0,
    cardID: 0,
    amount: 0,
    rechargeDate: new Date(),
    card: null
  }

  constructor(
    private _orderService : OrderService
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getCardOrdersObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._orderService.getCardOrders(currentPage! , itemsPerPage! , itemParams, this.cardID);


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
