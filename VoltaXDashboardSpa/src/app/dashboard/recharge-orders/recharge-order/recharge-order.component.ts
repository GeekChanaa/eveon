import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-recharge-order',
  templateUrl: './recharge-order.component.html',
  styleUrls: ['./recharge-order.component.sass']
})
export class RechargeOrderComponent implements OnInit {

  editingSuspension : boolean = false;

  order : any = {};

  constructor(
    private _orderService: OrderService,
    private _route: ActivatedRoute,
    private _modalService: ActionModalService
    ) { }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null){
      this.getOrder(parseInt(idParam));
    }
  }

  getOrder(id : number){
    this._orderService.getOrder(id).subscribe((data)=>{
      this.order = data;
    })
  }

}
