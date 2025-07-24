import { Injectable } from '@angular/core';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';
import { ChargePointBrand } from 'src/_models/charge-point-brand';

@Injectable({
  providedIn: 'root'
})
export class ChargePointBrandService extends AbstractService<ChargePointBrand>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/chargePointBrand/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/chargePointBrand/";

  getAllChargePointBrands(){
    return this._http.get<any>(this.baseUrl+"GetAllChargePointBrands/");
  }

}
