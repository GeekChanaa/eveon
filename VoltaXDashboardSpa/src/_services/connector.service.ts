import { Injectable } from '@angular/core';
import { Connector } from 'src/_models/connector';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class ConnectorService extends AbstractService<Connector>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http,snackBar, environment.apiUrl+"/api/connector");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/connector";

}
