import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable, map } from 'rxjs';

import { PaginatedResult } from 'src/_models/pagination';
import { AbstractService } from '../abstract-service';
import { OCPPLocalListVersion } from 'src/_models/ocpp-local-list/ocpp-local-list-version';
@Injectable({
  providedIn: 'root'
})
export class OCPPLocalListVersionService extends AbstractService<OCPPLocalListVersion>{

  constructor(
    protected http : HttpClient
    ) {
    super(http,environment.apiUrl+"/api/OCPPLocalListVersion/");
  }

  baseUrl = environment.apiUrl+"/api/OCPPLocalListVersion/";

  getOCPPLocalListVersionLocalList(chargePointID : number){
    return this._http.get<any[]>(this.baseUrl+"GetChargePointLocalList/"+chargePointID);
  }

  

}
