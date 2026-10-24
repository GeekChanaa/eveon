import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { CardService } from 'src/_services/card.service';
import { OrderService } from 'src/_services/order.service';

/** Credits a charging card by hand (cash, bank transfer, goodwill). Online payments go through the customer portal. */
@Component({
  selector: 'app-create-recharge-order',
  templateUrl: './create-recharge-order.component.html',
  styleUrls: ['./create-recharge-order.component.sass']
})
export class CreateRechargeOrderComponent implements OnInit {

    orderForm : FormGroup;
    isLoading : boolean = false;
    cards : { id: number; name: string; status: string }[] = [];
    readonly notePresets = ['Cash payment', 'Bank transfer', 'Commercial gesture', 'Correction'];

    constructor(
      private _cardService: CardService,
      private _modalService:  ActionModalService,
      private _orderService : OrderService,
      private _router : Router
    ) {
      this.orderForm = new FormGroup({
        cardID: new FormControl("", Validators.required),
        amount : new FormControl("", [Validators.required, Validators.min(0.01), Validators.max(100000)]),
        note : new FormControl("", [Validators.required, Validators.minLength(3), Validators.maxLength(256)])
      })
     }

    ngOnInit() {
      this.getAllCards();
    }

    onSubmit(){
      if (this.orderForm.invalid || this.isLoading) { this.orderForm.markAllAsTouched(); return; }
      this.isLoading = true;
      const order = this.orderForm.value;
      this._orderService.createRechargeOrder({ cardID: Number(order.cardID), amount: Number(order.amount), note: order.note.trim() }).subscribe({
        next: () => {
          this.isLoading = false;
          this._modalService.popup(ActionModalStatusEnum.Success, "Top-up recorded", "The card was credited.", 4000);
          this._router.navigateByUrl("/dashboard/recharge-orders");
        },
        error: (error) => {
          this.isLoading = false;
          this._modalService.popup(ActionModalStatusEnum.Error, "Top-up refused", error?.error?.message || "The card could not be credited.", 5000);
        }
      });
    }

    useNote(note: string) {
      this.orderForm.patchValue({ note });
    }

    getAllCards(){
      this._cardService.getAllCards(-1,-1).subscribe((data) => {
        if(data.result)
          this.cards = data.result.map((card: any) => ({ id: card.id, name: card.cardNumber, status: card.status }));
      })
    }

    getControl(name: string): FormControl {
      return this.orderForm.get(name) as FormControl;
    }

}
