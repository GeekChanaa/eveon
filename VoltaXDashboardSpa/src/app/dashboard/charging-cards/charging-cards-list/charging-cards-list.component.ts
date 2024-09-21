import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CardDto } from 'src/_models/_dtos/card-dto';
import { CardService } from 'src/_services/card.service';

@Component({
  selector: 'app-charging-cards-list',
  templateUrl: './charging-cards-list.component.html',
  styleUrls: ['./charging-cards-list.component.sass']
})
export class ChargingCardsListComponent implements OnInit {

  fields: string[] = [];
  filters : any = {
    category:"",
    city : ""
  };
  

  chargePoint: CardDto = {
    id: 0,
    cardNumber: '',
    userName: '',
    cardType: '',
    expirationDate: new Date(),
    maxCount: 0,
    status: '',
    balance: 0,
    note: '',
    userID: 0,
    user: null
  }

  // Constructor
  constructor(
    private _cardService: CardService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
  }

  getCardsObservable = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._cardService.getAllCards(currentPage, itemsPerPage, itemParams);
  deleteCardObservable = (id : number) => this._cardService.deleteById(id);
  updateCardObservable = (id : number, model : any) => this._cardService.edit(id, model);

  private _getItemFields() {
    if (!this.chargePoint || this.chargePoint == undefined) {
      return;
    }
    Object.keys(this.chargePoint ?? {}).forEach((element: string) => {
      if (typeof this.chargePoint?.[element] == "object" && this.chargePoint?.[element] != null && this.chargePoint?.[element].constructor.name == "Date")
        this.fields.push(element);
      if (typeof this.chargePoint?.[element] != "object") this.fields.push(element);
    });
  }
  

  resetFilters(){
    this.filters = {
      category:"",
      city : ""
    }
  }
}
