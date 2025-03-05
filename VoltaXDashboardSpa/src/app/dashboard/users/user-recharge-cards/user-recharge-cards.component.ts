import { Component, Input, OnInit } from '@angular/core';
import { CardListDto } from 'src/_models/_dtos/card-list-dto';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { CardTypeEnum } from 'src/_models/_enums/card-type';
import { CardService } from 'src/_services/card.service';

@Component({
  selector: 'app-user-recharge-cards',
  templateUrl: './user-recharge-cards.component.html',
  styleUrls: ['./user-recharge-cards.component.sass']
})
export class UserRechargeCardsComponent implements OnInit {

  @Input() userID : number = 0;
  
  constructor(
    private _cardService : CardService
  ) { }

  fields: string[] = [];
  filters : any = {
    role:""
  };
  

  card: CardListDto = {
    cardNumber: '',
    cardType: CardTypeEnum.Standard,
    expirationDate: '',
    maxCount: 0,
    status: CardStatusEnum.Active,
    balance: 0,
    note: '',
    userName: ''
  }


  ngOnInit() {
    this._getItemFields();
  }

  getUserCards = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._cardService.getUserRechargeCards(this.userID, currentPage ?? 0, itemsPerPage ?? -1, itemParams);

  private _getItemFields() {
    if (!this.card || this.card == undefined) {
      return;
    }
    Object.keys(this.card ?? {}).forEach((element: string) => {
      if (typeof this.card?.[element] == "object" && this.card?.[element] != null && this.card?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.card?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      role:""
    }
  }

}
