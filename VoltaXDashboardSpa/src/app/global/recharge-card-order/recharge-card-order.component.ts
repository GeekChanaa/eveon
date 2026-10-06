import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, FormBuilder } from '@angular/forms';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';
import { OrderService } from 'src/_services/order.service';

@Component({
  selector: 'app-recharge-card-order',
  templateUrl: './recharge-card-order.component.html',
  styleUrls: ['./recharge-card-order.component.css']
})
export class RechargeCardOrderComponent implements OnInit {

  form! : FormGroup;

  userID : number = 0;

  debitCards : any[] = [];
  rechargeCards: any[] = [];
  paymentStatuses = ['Completed', 'Pending', 'Processing', 'Failed', 'Canceled', 'Refunded'];
  paymentResult: any = null;
  paymentError = '';
  submittingPayment = false;

  currentStep = 1;
  constructor(
    private _fb: FormBuilder,
    private _authService : AuthService,
    private _cardService : CardService,
    private _userService : UserService,
    private _orderService: OrderService
  ) {
    this.form = new FormGroup({
      amount: new FormControl(''),
      cardID : new FormControl(''),
      mockPaymentStatus: new FormControl('Completed'),
      rechargeDate : new FormControl(''),
    });
   }

  ngOnInit() {
    var decodedToken = this._authService.getAuthInformation();
    this.userID = decodedToken.nameid;
    this._userService.getUserDebitCards(this.userID).subscribe((data) => {
      this.debitCards = data;
      console.log(this.debitCards)
    })
    this._cardService.getUserRechargeCards(this.userID, 1, 50).subscribe(data => {
      this.rechargeCards = data.result || [];
      if (this.rechargeCards.length === 1) this.form.patchValue({ cardID: this.rechargeCards[0].id });
    });
  }

  nextStep() {
    if (this.currentStep < 3) {
      this.currentStep++;
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
    }
  }

  submitPayment(): void {
    if (this.submittingPayment || this.form.invalid) return;
    this.submittingPayment = true;
    this.paymentError = '';
    const value = this.form.value;
    this._orderService.rechargeCard({
      CardID: Number(value.cardID), RechargeAmount: Number(value.amount), UserID: this.userID,
      SaveCard: false, MockPaymentStatus: value.mockPaymentStatus
    } as any).subscribe({
      next: result => { this.paymentResult = result; this.submittingPayment = false; },
      error: error => { this.paymentError = error?.error?.message || 'The mock payment could not be processed.'; this.submittingPayment = false; }
    });
  }

}
