import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationSetting } from 'src/_models/notification-setting';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';

@Injectable({
  providedIn: 'root'
})
export class NotificationSettingService  extends AbstractService<NotificationSetting>{
  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/notificationSetting/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/notificationSetting/";

  getUserNotificationSettings(userID : number){
    return this._http.get<any[]>(this.baseUrl+"UserNotificationSettings/"+userID);
  }

  // save user notification setting
  saveUserNotificationSetting(notificationSetting : any){
    return this._http.post(this.baseUrl+"SaveUserNotificationSetting",notificationSetting);
  }
}
