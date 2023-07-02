import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Card } from 'src/_models/card';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';

@Component({
  selector: 'app-my-card',
  templateUrl: './my-card.component.html',
  styleUrls: ['./my-card.component.css']
})
export class MyCardComponent implements OnInit {

  card : Card = {
    id: 0,
    cardNumber: '',
    cardType: '',
    expirationDate: new Date(),
    maxCount: 0,
    status: '',
    balance: 0,
    note: '',
    userID: 0,
    user: null
  }

  transactions : any[] = [];
  rechargeOrders : any[] = [];

  // card ID 
  cardID : number = 0;

  // constructor
  constructor(
    private _route : ActivatedRoute,
    private _authService : AuthService,
    private _cardService : CardService
  ) { }

  ngOnInit() {
    var cid = this._route.snapshot.paramMap.get('id');
    if(cid != null)
    this.cardID = parseInt(cid);

    this.getCard();
  }

  // Get Card
  getCard(){
    this._cardService.getById(this.cardID).subscribe((data) => {
      this.card = data;
      this.getCardTransactions();
      this.getCardTransactions();
    })
  }

  // get Card Transactions
  getCardTransactions(){
    this._cardService.getCardTransactions(this.card.id).subscribe((data) => {
      this.transactions = data;
    })
  }

  // get card recharge orders
  getCardRechargeOrders(){
    this._cardService.getCardOrders(this.card.id).subscribe((data) => {
      this.rechargeOrders = data;
    })
  }

}
