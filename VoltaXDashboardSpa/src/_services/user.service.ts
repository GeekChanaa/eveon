import { Injectable } from '@angular/core';
import { User } from 'src/_models/user';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService extends AbstractService<User>{

  constructor(protected http: HttpClient) {
    super(http, environment.apiUrl + "/api/user/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl + "/api/user/";

  // Get User Debit Cards
  getUserDebitCards(userID: number) {
    return this._http.get<any[]>(this.baseUrl + "GetUserDebitCards?UserID=" + userID)
      .pipe(
        map(data => data.map(item => {
          item.cardNumberHidden = this.transformCardNumber(item.cardNumberHidden);
        }))
      )
  }

  transformCardNumber(cardNumber: string | null): string | undefined {
    return cardNumber ? cardNumber.match(/.{1,4}/g)?.join(' ') : '';
  }
}
