import { Component, Input, OnInit } from '@angular/core';

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

  // Constructor
  constructor() { }

  ngOnInit() {
  }

}
