import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { subscribeOn } from 'rxjs';
import { Card } from 'src/_models/card';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-recharge-cards',
  templateUrl: './recharge-cards.component.html',
  styleUrls: ['./recharge-cards.component.css']
})
export class RechargeCardsComponent implements OnInit {

  @Input() userID : number = 0;

  showCreateForm : boolean = false;

  // card
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
  };
  // FormGroup
  form : FormGroup;
  rechargeCards : any[] = [];

  constructor(
    private _cardService : CardService,
    private _userService: UserService
  ) { 
    this.form = new FormGroup({
      cardType : new FormControl(''),
      expirationDate : new FormControl(''),
      maxCount : new FormControl(''),
      status : new FormControl(''),
      balance : new FormControl(''),
      note : new FormControl(''),
      userID : new FormControl('')
    })
  }

  // On init cycle hook
  ngOnInit() {
    console.log("this is the recharge cards comp");
    console.log(this.userID);
    this.getUserRechargeCards();
  }

  // Getting all recharge cards of the user
  getUserRechargeCards(){
    this._cardService.getUserRechargeCards(this.userID).subscribe((data) => {
      this.rechargeCards = data;
    })
  }

  // on submit button
  onSubmit(){
    var cardForm = this.form.value;
    this.card.cardType = cardForm.cardType;
    this.card.expirationDate = (new Date());
    this.card.maxCount = cardForm.maxCount;
    this.card.status = "active";
    this.card.note = cardForm.note;
    this.card.userID = this.userID;
    this.card.balance = 0;
    this.card.cardNumber = this.generateRandomString();
    this._cardService.create(this.card).subscribe((data) => {
      this.createFormHide();
      this.getUserRechargeCards();
    })
  }

  createFormShow(){
    this.showCreateForm = true;
  }

  createFormHide(){
    this.showCreateForm = false;
    this.form.reset();
  }

  generateRandomString(): string {
    let result = '';
    const characters = '0123456789';
    const charactersLength = characters.length;
    
    for (let i = 0; i < 16; i++) {
      result += characters.charAt(Math.floor(Math.random() * charactersLength));
    }
    
    return result;
  }

  // deleting recharge card
  delete(cardID : number){
    this._cardService.deleteById(cardID).subscribe();
  }
  

}
