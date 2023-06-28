import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CardDto } from 'src/_models/_dtos/card-dto';
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

  // item params 
  itemParams : any = {};

  card : CardDto = {
    id: 0,
    cardNumber: '',
    name : '',
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
    private _cardService : CardService,
    private _router : Router
  ) { }

  ngOnInit() {
    this._getItemFields();
    this.getAll();
  }

  
  // Getting All recharge cards
  getAll(){
    this._cardService.getAllCards(this.currentPage,this.itemsPerPage).subscribe(data => {
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

  delete(id : number){
    this._cardService.deleteById(id).subscribe((data) => {
      this.getAll();
    })
  }

  display(id : number){
    this._router.navigate(['/charging-cards/',id]);
  }

  update(id : number){
    console.log("updated");
  }

  // sorting by field
  sort(field : string){
    if(this.itemParams.orderBy == field){
      if(this.itemParams.reverseOrder == 'y')
      this.itemParams.reverseOrder = 'n'
      else
      this.itemParams.reverseOrder = 'y'
    }
    else{
      this.itemParams.orderBy = field;
      this.itemParams.reverseOrder = 'n'
    }
    this.getAll();
  }

}
