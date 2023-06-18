import { Injectable } from '@angular/core';
import { User } from 'src/_models/user';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService extends AbstractService<User>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/user/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/user/";

}
