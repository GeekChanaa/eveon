import { Injectable } from '@angular/core';
import { User } from 'src/_models/user';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserNameDto } from 'src/_models/_dtos/user-name-dto';

@Injectable({
  providedIn: 'root'
})
export class UserService extends AbstractService<User>{

  constructor(protected http: HttpClient, snackBar : MatSnackBar) {
    super(http, snackBar , environment.apiUrl + "/api/user/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl + "/api/user/";

  // Get User Debit Cards
  getUserDebitCards(userID: number) {
    return this._http.get<any[]>(this.baseUrl + "GetUserDebitCards?UserID=" + userID);
  }

  // user email unique
  userEmailExists(email : string) : Observable<Boolean>{
    return this._http.get<Boolean>(this.baseUrl + "UserEmailExists/?email=" + email);
  }

  // user phone unique
  userPhoneExists(phone : string) : Observable<Boolean>{
    return this._http.get<Boolean>(this.baseUrl + "UserPhoneExists/?phone=" + phone);
  }

  // Getting all user names
  getUserNames(){
    return this._http.get<UserNameDto[]>(this.baseUrl + "GetUserNames")
  }

  // getting user names by name
  getAllUsersNamesByName(name: string){
    return this._http.get<UserNameDto[]>(this.baseUrl + "GetUserNamesByName?name="+name);
  }
  

}
