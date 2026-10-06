import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
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
    cards : { id: number; name: string; userID: number }[] = [];
    readonly paymentStatuses = ['Completed', 'Pending', 'Processing', 'Failed', 'Canceled', 'Refunded'];

    constructor(
      private _cardService: CardService,
      private _modalService:  ActionModalService,
      private _orderService : OrderService,
      private _router : Router
    ) {
      this.orderForm = new FormGroup({
        cardID: new FormControl("", Validators.required),
        status : new FormControl("Completed", Validators.required),
        amount : new FormControl("", [Validators.required, Validators.min(0.01)])
      })
     }
  
    ngOnInit() {
      this.getAllCards();
    }
  
    onSubmit(){
      if (this.orderForm.invalid || this.isLoading) { this.orderForm.markAllAsTouched(); return; }
      const selectedCard = this.cards.find(card => card.id === Number(this.orderForm.value.cardID));
      if (!selectedCard) return;
      this.isLoading = true;
      const order = this.orderForm.value;
      this._orderService.rechargeCard({ CardID: selectedCard.id, RechargeAmount: Number(order.amount), UserID: selectedCard.userID, SaveCard: false, MockPaymentStatus: order.status }).subscribe((data: any) => {
        this.isLoading = false;
        this._modalService.popup(ActionModalStatusEnum.Success, "Payment recorded", data.message || "The recharge payment was recorded.", 4000);
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
          this.cards = data.result.map((card: any) => ({ id: card.id, name: card.cardNumber, userID: card.userID }));
      })
    }
  
    getControl(name: string): FormControl {
      return this.orderForm.get(name) as FormControl;
    }

}
