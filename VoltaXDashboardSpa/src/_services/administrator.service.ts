import { Injectable } from '@angular/core';
import { Administrator } from 'src/_models/administrator';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class AdministratorService extends AbstractService<Administrator>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/administrator");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/administrator";

}
