import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';
import { ChargePointModel } from 'src/_models/charge-point-model';

@Injectable({
  providedIn: 'root'
})
export class ChargePointModelService extends AbstractService<ChargePointModel>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargePointModel/");
  }

  baseUrl = environment.apiUrl+"/api/chargePointModel/";


  getAllChargePointModels(){
    return this._http.get<any>(this.baseUrl+"GetAllChargePointModels/");
  }

}
