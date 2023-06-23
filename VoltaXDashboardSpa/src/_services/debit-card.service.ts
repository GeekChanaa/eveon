import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { DebitCard } from 'src/_models/debit-card';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class DebitCardService extends AbstractService<DebitCard>{
  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/debitCard");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/debitCard";

}
