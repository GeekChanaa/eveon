import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageState } from 'src/_models/_enums/page-state.enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OrderService } from 'src/_services/order.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-recharge-order',
  templateUrl: './recharge-order.component.html',
  styleUrls: ['./recharge-order.component.sass']
})
export class RechargeOrderComponent implements OnInit {


  updateOrderObservable = (id : number, model : any) => this._orderService.edit(id, model);

  PageState = PageState;
  state: PageState = PageState.Loading;
  
  order: any = {};

  staticUrl : string = environment.apiStaticFilesUrl;

  // Form group
  orderForm : FormGroup;

  constructor(
    private _orderService: OrderService,
    private _route: ActivatedRoute,
  ) {
    this.orderForm = new FormGroup({
      serialNumber : new FormControl(''),
      make : new FormControl(''),
      status : new FormControl(''),
      category : new FormControl(''),
      comment : new FormControl(''),
      chargePointCategory : new FormControl(''),
    })
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getRechargeOrderByID(id);
    }
  }


  getRechargeOrderByID(id : number){
    this.state = PageState.Loading;
    this._orderService.getOrder(id).subscribe((cs) => {
      this.order = cs;
      this.state = PageState.Success
    })
  }

  downloadQuote(id:number){
    this._orderService.getOrderInvoice(id);
  }



}
