import { Component, Input, OnInit } from '@angular/core';
import { subscribeOn } from 'rxjs';
import { CardService } from 'src/_services/card.service';
import { CustomerService } from 'src/_services/customer.service';

@Component({
  selector: 'app-recharge-cards',
  templateUrl: './recharge-cards.component.html',
  styleUrls: ['./recharge-cards.component.css']
})
export class RechargeCardsComponent implements OnInit {

  @Input() userID : number = 0;

  constructor(
    private _cardService : CardService,
    private _customerService: CustomerService
  ) { }

  // On init cycle hook
  ngOnInit() {
    console.log("this is the recharge cards comp");
    console.log(this.userID);
    this.getCustomerRechargeCards();
  }

  // Getting all recharge cards of the user
  getCustomerRechargeCards(){
    this._customerService.getCustomerByUserID(this.userID).subscribe((data) => {
      console.log("this is the customerID :");
    })
  }

}
