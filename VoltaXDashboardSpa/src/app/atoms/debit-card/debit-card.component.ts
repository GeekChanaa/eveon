import { Component, EventEmitter, Input, OnInit, Output, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-debit-card',
  templateUrl: './debit-card.component.html',
  styleUrls: ['./debit-card.component.sass']
})
export class DebitCardComponent implements OnInit {


  showDeleteConfirmModal : boolean = false;

  ngOnInit() {
    console.log(this.type);
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
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['cardNumber'] && this.cardNumber) {
      this.detectCardType();
    }
  }

  detectCardType(): void {
    const cleanNumber = this.cardNumber.replace(/\s+/g, '');
    
    if (/^4/.test(cleanNumber)) {
      this.cardType = 'visa';
    } 
    else if (/^5[1-5]/.test(cleanNumber) || /^2[2-7][2-7]\d{2}/.test(cleanNumber)) {
      this.cardType = 'mastercard';
    } 
    else {
      this.cardType = 'visa';
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
