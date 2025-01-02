import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ChargingSession } from 'src/_models/charging-session';


@Injectable({
  providedIn: 'root'
})
export class ChargingSessionService extends AbstractService<ChargingSession> {

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargingSession/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargingSession/";

  getChargePointChargingSessions(chargePointID : number,page : number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetChargePointChargingSessions/"+chargePointID);
  }

}
