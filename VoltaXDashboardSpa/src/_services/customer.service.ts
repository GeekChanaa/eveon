import { Injectable } from '@angular/core';
import { Customer } from 'src/_models/customer';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CustomerService extends AbstractService<Customer>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/customer/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/customer/";

  // Get Customers Names
  getCustomersNames(){
    return this._http.get<any[]>(this.baseUrl+"GetAllCustomersNames");
  }

  // get CustomersName by name
  getCustomersNamesByName(name : string){
    return this._http.get<any[]>(this.baseUrl+"GetAllCustomersNamesByName?name="+name);
  }

}
