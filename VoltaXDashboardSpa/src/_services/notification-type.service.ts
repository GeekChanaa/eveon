import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { NotificationType } from 'src/_models/notification-type';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';

@Injectable({
  providedIn: 'root'
})
export class NotificationTypeService  extends AbstractService<NotificationType>{
  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/notificationType/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/notificationType/";

  // Getting notification Types by Role
  getNotificationTypesBy(role : number){
    return this._http.get<any[]>(this.baseUrl+"NotificationTypeFor?role="+role);
  }

}
