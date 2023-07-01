import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from 'src/_services/auth.service';
import { CardService } from 'src/_services/card.service';
import { UserService } from 'src/_services/user.service';

@Component({
  selector: 'app-my-cards',
  templateUrl: './my-cards.component.html',
  styleUrls: ['./my-cards.component.css']
})
export class MyCardsComponent implements OnInit {

  rechargeCards : any[] = [];

  // constructor
  constructor(
    private _authService : AuthService,
    private _cardService : CardService,
    private _userService : UserService
  ) { }

  ngOnInit() {
    this.getRechargeCards();
  }

  // getting user recharge cards
  getRechargeCards(){
    var userID = this._authService.getAuthInformation().nameid;
    this._cardService.getUserRechargeCards(parseInt(userID)).subscribe((data) => {
      this.rechargeCards = data;
    })
  }

}
