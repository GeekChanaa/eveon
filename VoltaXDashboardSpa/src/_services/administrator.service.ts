import { Injectable } from '@angular/core';
import { Administrator } from 'src/_models/administrator';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdministratorService extends AbstractService<Administrator>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/administrator");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"api/administrator";

}
