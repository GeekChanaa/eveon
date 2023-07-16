import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-debit-card',
  templateUrl: './debit-card.component.html',
  styleUrls: ['./debit-card.component.css']
})
export class DebitCardComponent implements OnInit {

  // Input parameters for the component
  @Input() cardNumber : string = "";
  @Input() cardHolderName : string = "";
  @Input() size : string = "";
  @Input() id : number = 0;
  @Output() deleteEvent : EventEmitter<number> = new EventEmitter<number>();

  constructor() { }

  ngOnInit() {
  }

  delete(id : number ){
    this.deleteEvent.emit(id);
  }

}
