import { Component, OnInit } from '@angular/core';
import { ChargingCardsComponent } from '../charging-cards.component';
import { FormControl, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-create-charging-card',
  templateUrl: './create-charging-card.component.html',
  styleUrls: ['./create-charging-card.component.css']
})
export class CreateChargingCardComponent implements OnInit {

  // FormGroup
  form : FormGroup;

  constructor(
    private _chargingCardService:  ChargingCardsComponent
  ) { 
    this.form = new FormGroup({
      cardNumber : new FormControl(''),
      account : new FormControl(''),
      cardType : new FormControl(''),
      expirationDate : new FormControl(''),
      maxCount : new FormControl(''),
      status : new FormControl(''),
      balance : new FormControl(''),
      note : new FormControl(''),
      customerID : new FormControl('')
    })
  }

  ngOnInit() {
    
  }

}
