import { Component, Input, OnInit } from '@angular/core';
import { subscribeOn } from 'rxjs';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-recharge-cards',
  templateUrl: './recharge-cards.component.html',
  styleUrls: ['./recharge-cards.component.css']
})
export class RechargeCardsComponent implements OnInit {

  @Input() userID : number = 0;

  constructor(
    private _cardService : CardService,
    private _userService: UserService
  ) { }

  // On init cycle hook
  ngOnInit() {
    console.log("this is the recharge cards comp");
    console.log(this.userID);
    this.getUserRechargeCards();
  }

  // Getting all recharge cards of the user
  getUserRechargeCards(){
    
  }

}
