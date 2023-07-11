import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ConnectorStatus } from 'src/_models/connector-status';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class ConnectorStatusService extends AbstractService<ConnectorStatus> {

  constructor(protected http : HttpClient, snackBar : MatSnackBar) {
    super(http, snackBar, environment.apiUrl+"/api/connectorStatus/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/connectorStatus/";

  // Get number of connectors by status
  getNumberOfConnectorsByStatus(status : string){
    return this.http.get<number>(this.baseUrl+"GetNumberOfConnectorsByStatus?status="+status);
  }

  // Get number of connectors by status
  getPartnerNumberOfConnectorsByStatus(status : string){
    return this.http.get<number>(this.baseUrl+"GetPartnerNumberOfConnectorsByStatus?status="+status);
  }
}
