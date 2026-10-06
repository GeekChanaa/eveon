import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-recharge-card',
  templateUrl: './recharge-card.component.html',
  styleUrls: ['./recharge-card.component.sass']
})
export class RechargeCardComponent {
  @Input() cardNumber: string = '1234567890123456';
  @Input() cardType: string = 'Premium Charging';
  @Input() expirationDate: string = '12/26';
  @Input() status: string = 'Active';
  @Input() balance: number = 85.50;

  showCardNumber: boolean = false;
  showBalance: boolean = false;

  toggleCardNumberVisibility(): void {
    this.showCardNumber = !this.showCardNumber;
  }

  toggleBalanceVisibility(): void {
    this.showBalance = !this.showBalance;
  }

  formatCardNumber(cardNumber: string): string {
    return (cardNumber || '').replace(/\s/g, '').replace(/(.{4})/g, '$1 ').trim();
  }

  formatBalance(balance: number): string {
    return `${Number(balance || 0).toFixed(2)} MAD`;
  }

  formatExpirationDate(date: string): string {
    return date;
  }
}
