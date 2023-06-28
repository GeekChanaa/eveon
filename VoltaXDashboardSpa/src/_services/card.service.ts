import { Injectable } from '@angular/core';
import { Card } from 'src/_models/card';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class CardService extends AbstractService<Card>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http, snackBar, environment.apiUrl+"/api/card/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/card/";

  // Get user recharge cards
  getUserRechargeCards(userID : number){
    return this._http.get(this.baseUrl+"GetUserRechargeCards?UserID="+userID);
  }

  // get all cards
  getAllCards(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetAllCards");
  }

  

}
