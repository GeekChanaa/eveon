import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ConnectorUptime } from 'src/_models/connector-uptime';


@Injectable({
  providedIn: 'root'
})
export class ConnectorUptimeService extends AbstractService<ConnectorUptime> {

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/connectorUptime/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/connectorUptime/";

  getConnectorUptime(connectorID : number,page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetConnectorUptime/"+connectorID);
  }
}
