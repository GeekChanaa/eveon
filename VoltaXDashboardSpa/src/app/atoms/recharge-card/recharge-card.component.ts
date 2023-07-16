import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { Card } from 'src/_models/card';

@Component({
  selector: 'app-recharge-card',
  templateUrl: './recharge-card.component.html',
  styleUrls: ['./recharge-card.component.css']
})
export class RechargeCardComponent implements OnInit {

  @Output() deleteEvent : EventEmitter<void> = new EventEmitter<void>();

  @Input() card : Card = {
    id: 0,
    cardNumber: '',
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

  @Input() small : boolean = false;

  

  // constructor
  constructor() { }

  // on init
  ngOnInit() {
  }

  // remove recharge card button
  remove(){
    this.deleteEvent.emit();
  }

}
