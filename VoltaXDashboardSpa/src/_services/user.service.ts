import { Injectable } from '@angular/core';
import { User } from 'src/_models/user';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { BehaviorSubject, Observable, map } from 'rxjs';

import { UserNameDto } from 'src/_models/_dtos/user-name-dto';
import { UserDashboardEditInformationsDto } from 'src/_models/_dtos/users-dtos/user-dashboard-edit-informations-dto';
import { UserListDto } from 'src/_models/_dtos/user-list-dto';

@Injectable({
  providedIn: 'root'
})
export class UserService extends AbstractService<User>{

  private avatarUrlSubject = new BehaviorSubject<string | null>(null);
  // Method to update avatar URL
  setAvatarUrl(url: string): void {
    this.avatarUrlSubject.next(url);
  }

  // Method to get avatar URL as an observable
  getAvatarUrl(): Observable<string | null> {
    return this.avatarUrlSubject.asObservable();
  }
  
  constructor(protected http: HttpClient) {
    super(http,environment.apiUrl + "/api/user/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl + "/api/user/";

  // Get User Debit Cards
  getUserDebitCards(userID: number) {
    return this._http.get<any[]>(this.baseUrl + "GetUserDebitCards?UserID=" + userID);
  }

  // Get User Recharge Cards
  getUserRechargeCards(userID: number) {
    return this._http.get<any[]>(this.baseUrl + "GetUserRechargeCards?UserID=" + userID);
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
  getUserNames() {
    return this._http.get<any[]>(this.baseUrl + "GetUserNames").pipe(
      map(users => users.map(user => ({
        id: user.id,
        name: user.fullName 
      })))
    );
  }

  getAllUsers(page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetUsers");
  }

  getAllUsersNamesByName(name: string){
    return this._http.get<any[]>(this.baseUrl + "GetUserNamesByName?name="+name);
  }

  isEmailUnique(email:string){
    return this._http.get<any>(this.baseUrl + "IsEmailUnique/"+email);
  }

  isPhoneUnique(phone:string){
    return this._http.get<any>(this.baseUrl + "IsPhoneUnique/"+phone);
  }

  getSupportUserNames(){
    return this._http.get<any>(this.baseUrl + "GetSupportUserNames/");
  }

  GetPartnerNames(){
    return this._http.get<any[]>(this.baseUrl + "GetPartnerNames").pipe(
      map(users => users.map(user => ({
        id: user.id,
        name: user.fullName 
      })))
    );
  }

  getUserDashboardDisplayInformations(id : number){
    return this._http.get<any>(this.baseUrl + "GetUserDashboardDisplayInformations/"+id);
  }

  editUserDashboardInformations(id : number, user: UserDashboardEditInformationsDto){
    return this._http.put<any>(this.baseUrl + "EditUserDashboardInformations/"+id,user);
  }

  getUserInformations(userID : number){
    return this._http.get<any>(this.baseUrl + "GetUserInformations/"+userID);
  }

  downloadUserInformations(user : UserListDto){
    // Format the content
    const content = `User Informations\n\n` +
                    `First Name: ${user.firstName}\n` +
                    `Last Name: ${user.lastName}\n` +
                    `Email: ${user.email}\n` +
                    `Phone: ${user.phone}\n` +
                    `Car: ${user.car}\n` +
                    `Birthday: ${user.birthday}\n` +
                    `City: ${user.city}\n` +
                    `Gender: ${user.gender}\n`;
  
    // Create a Blob
    const blob = new Blob([content], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
  
    // Create a link and trigger the download
    const a = document.createElement('a');
    a.href = url;
    a.download = 'user_info.txt';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  
    // Cleanup
    window.URL.revokeObjectURL(url);
  }

  uploadUserAvatar(formData : FormData, userID:  number){
    return this.http.post<any[]>(this.baseUrl + 'UploadUserAvatar/'+userID, formData, { reportProgress: true, observe: 'events' });
  }

  createUserDashboard(userToCreate : any){
    return this.http.post<any>(this.baseUrl + 'CreateUserDashboard', userToCreate);
  }



  

}
