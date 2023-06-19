import { Component, OnInit } from '@angular/core';
import { Card } from 'src/_models/card';
import { CardService } from 'src/_services/card.service';

@Component({
  selector: 'app-charging-cards',
  templateUrl: './charging-cards.component.html',
  styleUrls: ['./charging-cards.component.css']
})
export class ChargingCardsComponent implements OnInit {

  // Data
  data : any[] = [];

  // Fields
  fields : string[] = [];

  // Page params
  itemsPerPage : number = 20;
  currentPage : number = 1;

  card : Card = {
    id: 0,
    cardNumber: '',
    account: '',
    cardType: '',
    expirationDate: new Date(),
    maxCount: 0,
    status: '',
    balance: 0,
    note: '',
    customerID: 0,
    customer: null
  }

  // Constructor
  constructor(
    private _cardService : CardService
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All Products
  getAll(){
    this._cardService.getAll(this.currentPage,this.itemsPerPage).subscribe(data => {
      if (data.result) {
        this.data = data.result;
      }
    })
  }

  // Getting Item Fields
  private _getItemFields(){
    // Ensure this.chargingStation is defined
    if (!this.card || this.card == undefined) {
      return;
    }
    // Getting item fields
    Object.keys(this.card ?? {}).forEach((element : string) => {
      console.log(element); 
      if(typeof this.card?.[element] == "object" && this.card?.[element] != null && this.card?.[element].constructor.name == "Date")
      this.fields.push(element);
      if(typeof this.card?.[element] != "object") this.fields.push(element);
    });
  }

  // Deleting the item

  // Next page
  nextPage(){
    this.currentPage++;
    this.getAll();
  }

  // Previous Page
  previousPage(){
    this.currentPage--;
    this.getAll();
  }

}
