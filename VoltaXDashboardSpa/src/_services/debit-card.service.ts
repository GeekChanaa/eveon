import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { AddDebitCardRequest, DebitCard } from 'src/_models/debit-card';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';


@Injectable({
  providedIn: 'root'
})
export class DebitCardService extends AbstractService<DebitCard>{
  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/debitCard/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/debitCard/";

  // Saves a card from a payment-provider token; the API rejects card numbers and CVVs.
  addCard(request: AddDebitCardRequest) {
    return this.http.post<any>(this.baseUrl, request, this.httpOptions);
  }

}
