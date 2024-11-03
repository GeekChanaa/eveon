import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { ChargePointUptime } from 'src/_models/charge-point-uptime';


@Injectable({
  providedIn: 'root'
})
export class ChargePointUptimeService extends AbstractService<ChargePointUptime> {

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargePointUptime/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargePointUptime/";

  getChargePointUptime(chargePointID : number,page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetChargePointUptime/"+chargePointID);
  }

}
