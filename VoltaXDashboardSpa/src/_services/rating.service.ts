import { Injectable } from '@angular/core';
import { Rating } from 'src/_models/rating';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RatingService extends AbstractService<Rating>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/Rating/");
  }

  baseUrl = environment.apiUrl+"/api/Rating/";
  
  getChargePointRatings(chargePointID: number ,page?: number, itemsPerPage?: number, itemParams?: any){
    return this.getAll(page, itemsPerPage, itemParams, "GetChargePointRatings/"+chargePointID)
  }

}
