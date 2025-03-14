import { Injectable } from '@angular/core';
import { Order } from 'src/_models/order';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { RechargeOrderDto } from 'src/_models/_dtos/recharge-order-dto';
import { InvoiceDTO } from 'src/_models/_dtos/invoice-dto';
import { Observable } from 'rxjs';
import { OCPPConfigurationItem } from 'src/_models/ocpp-configuration-item';

@Injectable({
  providedIn: 'root'
})
export class OCPPConfigurationItemService extends AbstractService<OCPPConfigurationItem>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/OCPPConfigurationItem/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/OCPPConfigurationItem/";

  getChargePointConfigurationItems(chargePointID : number, page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"GetChargePointConfigurationItems/"+chargePointID);
  }
}
