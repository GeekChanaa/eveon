import { Injectable } from '@angular/core';
import { Connector } from 'src/_models/connector';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { map } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class ConnectorService extends AbstractService<Connector>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/connector/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/connector/";

  getChargePointConnectors(id : number){
    return this._http.get<any[]>(this.baseUrl+"GetChargePointConnectors/"+id);
  }

  getConnectorIds(){
    return this._http.get<any[]>(this.baseUrl+"GetConnectorsIds/").pipe(
      map(connectors => connectors.map(connector => ({
        id: connector.id,
        name: connector.chargePointID + " " + connector.connectorID
      })))
    );
  }

  updateConnectorPricing(connectorID : number, updateConnectorPricingDto : any){
    return this._http.post<any>(this.baseUrl+"UpdateConnectorPricing/"+connectorID, updateConnectorPricingDto)
  }

}
