import { Injectable } from '@angular/core';
import { Order } from 'src/_models/order';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService extends AbstractService<Order>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/order");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"api/order";

}
