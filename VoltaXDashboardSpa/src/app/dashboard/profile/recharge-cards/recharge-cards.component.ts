import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { subscribeOn } from 'rxjs';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { CardTypeEnum } from 'src/_models/_enums/card-type';
import { Card } from 'src/_models/card';
import { CardService } from 'src/_services/card.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-recharge-cards',
  templateUrl: './recharge-cards.component.html',
  styleUrls: ['./recharge-cards.component.sass']
})
export class RechargeCardsComponent implements OnInit {

  @Input() userID : number = 0;

  cardTypes : any = {};

  // card
  card : Card = {
    id: 0,
    cardNumber: '',
    cardType: CardTypeEnum.Standard,
    expirationDate: new Date(),
    maxCount: 0,
    status: CardStatusEnum.Inactive,
    balance: 0,
    note: '',
    userID: 0,
    user: null,
    transactions : [],
    orders : []
  };
  // FormGroup
  rechargeCards : any[] = [];

  constructor(
    private _cardService : CardService,
    private _userService: UserService,
    private _enumMapping : EnumMappingService
  ) { 
  }

  // On init cycle hook
  ngOnInit() {
    this.cardTypes = this._enumMapping.getEnumMapping("CardType");
    this.getUserRechargeCards();
  }

  // Getting all recharge cards of the user
  getUserRechargeCards(){
  }


  
  

}
