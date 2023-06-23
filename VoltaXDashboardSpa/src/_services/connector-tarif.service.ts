import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ConnectorTarif } from 'src/_models/connector-tarif';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class ConnectorTarifService extends AbstractService<ConnectorTarif>{

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http, snackBar, environment.apiUrl+"/api/connectorTarif");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/connectorTarif";

}
