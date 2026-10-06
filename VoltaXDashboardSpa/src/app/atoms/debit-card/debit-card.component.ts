import { Component, EventEmitter, Input, OnInit, Output, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-debit-card',
  templateUrl: './debit-card.component.html',
  styleUrls: ['./debit-card.component.sass']
})
export class DebitCardComponent implements OnInit {


  showDeleteConfirmModal : boolean = false;

  ngOnInit() {
  }

  cancelDelete(){
    this.showDeleteConfirmModal= false;
  }


  proceedWithDelete(){
    this.delete(this.id);
  }

  @Input() cardNumber : string = "";
  @Input() cardHolderName : string = "";
  @Input() size : string = "";
  @Input() type : string = "";
  @Input() id : number = 0;
  
  @Input() expiryDate: string = "";
  @Input() cardType: 'mastercard' | 'visa' | 'unknown' = 'unknown';
  
  @Output() deleteCard = new EventEmitter<number>();
  
  // The brand comes from the API (the full number is never available to detect it from).
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['type']) {
      const brand = (this.type || '').toLowerCase();
      this.cardType = brand === 'visa' ? 'visa' : brand === 'mastercard' ? 'mastercard' : 'unknown';
    }
  }

  delete(id: number): void {
    this.deleteCard.emit(id);
  }
  
  formatCardNumber(number: string): string {
    if (!number) return '';
    // Format card number in groups of 4 digits
    const cleaned = number.replace(/\s+/g, '');
    const groups = cleaned.match(/.{1,4}/g);
    return groups ? groups.join(' ') : cleaned;
  }
}
