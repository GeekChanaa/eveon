import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, FormBuilder } from '@angular/forms';
import { DebitCard } from 'src/_models/debit-card';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-recharge-card-order',
  templateUrl: './recharge-card-order.component.html',
  styleUrls: ['./recharge-card-order.component.css']
})
export class RechargeCardOrderComponent implements OnInit {

  form! : FormGroup;

  userID : number = 0;

  newDebitCard : boolean = false;

  debitCards : any[] = [];

  currentStep = 1;
  constructor(
    private _fb: FormBuilder,
    private _authService : AuthService,
    private _cardService : CardService,
    private _userService : UserService
  ) {
    this.form = new FormGroup({
      amount: new FormControl(''),
      cardID : new FormControl(''),
      rechargeDate : new FormControl(''),
      debitCard: this._fb.group({
        holderName: new FormControl(''),
        cardNumber: new FormControl(''),
        expiryDate : new FormControl(''),
        cvv : new FormControl('')
      })
    });
   }

  ngOnInit() {
    var decodedToken = this._authService.getAuthInformation();
    this.userID = decodedToken.nameid;
    this._userService.getUserDebitCards(this.userID).subscribe((data) => {
      this.debitCards = data;
      console.log(this.debitCards)
    })
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

  addNewDebitCard(){
    this.newDebitCard = true;
  }

}
