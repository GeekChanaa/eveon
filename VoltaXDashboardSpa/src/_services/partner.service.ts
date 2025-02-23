import { Injectable } from '@angular/core';
import { Partner } from 'src/_models/partner';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PartnerService extends AbstractService<Partner>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/partner/");
  }

  baseUrl = environment.apiUrl+"/api/partner/";

  
}
