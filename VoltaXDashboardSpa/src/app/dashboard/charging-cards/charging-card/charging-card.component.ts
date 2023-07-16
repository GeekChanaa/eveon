import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { Card } from 'src/_models/card';
import { CardService } from 'src/_services/card.service';

enum ChargingCardTabsEnum {
  InformationsTab = "InformationsTab",
  Transactions = "Transactions",
  Orders = "Orders",
}
@Component({
  selector: 'app-charging-card',
  templateUrl: './charging-card.component.html',
  styleUrls: ['./charging-card.component.css']
})
export class ChargingCardComponent implements OnInit {

  // TabsEnum
  tabsEnum : ChargingCardTabsEnum = ChargingCardTabsEnum.InformationsTab;

  // Charging Card
  chargingCard : Card = {
    id: 0,
    cardNumber: '',
    account: '',
    cardType: '',
    expirationDate: new Date(),
    maxCount: 0,
    status: CardStatusEnum.Inactive,
    balance: 0,
    note: '',
    userID: 0,
    user: null,
    transactions : [],
    orders : []
  }

  // charging card id
  chargeCardID : number = 0;

  constructor(
    private _cardService : CardService,
    private _router : ActivatedRoute
  ) { }

  ngOnInit() {
    var idParam = this._router.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.chargeCardID = id;
      this._cardService.getById(id).subscribe((cs) => {
        this.chargingCard = cs;
      })
    }
  }

  // Changing current tab
  changeTab(tab : any){
    this.tabsEnum = tab;
  }

  update(vale : any,name : string){
    this.chargingCard[name] = vale;
    this.chargingCard.transactions=[];
    this.chargingCard.orders=[];
    this._cardService.edit(this.chargingCard.id, this.chargingCard).subscribe((data) => {
      this.getchargingCardByID(this.chargingCard.id);
    })
  }

  getchargingCardByID(id : number){
    this._cardService.getById(id).subscribe((data) => {
      this.chargingCard = data;
    })
  }
}
