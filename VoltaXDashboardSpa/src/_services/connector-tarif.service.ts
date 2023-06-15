import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ConnectorTarif } from 'src/_models/connector-tarif';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';

@Injectable({
  providedIn: 'root'
})
export class ConnectorTarifService extends AbstractService<ConnectorTarif>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/connectorTarif");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/connectorTarif";

}
