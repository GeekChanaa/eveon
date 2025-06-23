import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CardStatusEnum } from 'src/_models/_enums/card-status';
import { CardTypeEnum } from 'src/_models/_enums/card-type';
import { Card } from 'src/_models/card';

@Component({
  selector: 'app-recharge-card',
  templateUrl: './recharge-card.component.html',
  styleUrls: ['./recharge-card.component.sass']
})
export class RechargeCardComponent implements OnInit {
  ngOnInit(): void {

  }

  @Input() cardNumber: string = '1234567890123456';
  @Input() cardType: string = 'Premium Charging';
  @Input() expirationDate: string = '12/26';
  @Input() status: string = 'Active';
  @Input() balance: number = 85.50;

  showCardNumber: boolean = false;

  toggleCardVisibility(): void {
    this.showCardNumber = !this.showCardNumber;
  }

  formatCardNumber(cardNumber: string): string {
    return cardNumber.replace(/(.{4})/g, '$1 ').trim();
  }

  formatBalance(balance: number): string {
    return `$${balance.toFixed(2)}`;
  }

  formatExpirationDate(date: string): string {
    return date;
  }
}
