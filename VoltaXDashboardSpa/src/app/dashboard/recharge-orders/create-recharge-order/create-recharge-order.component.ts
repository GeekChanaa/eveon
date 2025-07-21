import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { CardService } from 'src/_services/card.service';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-create-recharge-order',
  templateUrl: './create-recharge-order.component.html',
  styleUrls: ['./create-recharge-order.component.sass']
})
export class CreateRechargeOrderComponent implements OnInit {
  
    orderForm : FormGroup;
    isLoading : boolean = false;
    cards : any[] = []

    constructor(
      private _cardService: CardService,
      private _modalService:  ActionModalService,
      private _orderService : OrderService,
      private _router : Router
    ) {
      this.orderForm = new FormGroup({
        cardID: new FormControl(""),
        status : new FormControl("Pending"),
        amount : new FormControl("")
      })
     }
  
    ngOnInit() {
      this.getAllCards();
    }
  
    onSubmit(){
      this.isLoading = true;
      var order = this.orderForm.value;
      this._orderService.createRechargeOrder(order).subscribe((data) => {
        this.isLoading = false;
        this._modalService.popup(ActionModalStatusEnum.Success, "Success", "The Connector has been added succesfully", 4000);
        this._router.navigateByUrl("/dashboard/recharge-orders");
      },(error) => {
        this.isLoading = false;
        this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong", 4000);
      })
    }

    getAllCards(){
      this._cardService.getAllCards(-1,-1).subscribe((data) => {
        console.log(data.result);
        if(data.result)
          this.cards = data.result.map((card) => ({id : card.id, name : card.cardNumber}));
      })
    }
  
    getControl(name: string): FormControl {
      return this.orderForm.get(name) as FormControl;
    }

}
