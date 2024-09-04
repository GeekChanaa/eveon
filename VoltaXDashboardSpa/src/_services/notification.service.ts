import { Injectable } from '@angular/core';
import { Notification } from 'src/_models/notification';

@Injectable({
  providedIn: 'root'
})
export class NotificationService  extends AbstractService<Notification>{
  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/notification/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/notification/";

}