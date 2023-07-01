import { Component, OnInit, Input } from '@angular/core';
import { Card } from 'src/_models/card';

@Component({
  selector: 'app-recharge-card',
  templateUrl: './recharge-card.component.html',
  styleUrls: ['./recharge-card.component.css']
})
export class RechargeCardComponent implements OnInit {

  @Input() card : Card = {
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

  

  // constructor
  constructor() { }

  // on init
  ngOnInit() {
  }

}
