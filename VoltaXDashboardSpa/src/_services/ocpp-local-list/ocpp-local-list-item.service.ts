import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { AbstractService } from '../abstract-service';
import { OCPPLocalListItem } from 'src/_models/ocpp-local-list/ocpp-local-list-item';

@Injectable({
  providedIn: 'root'
})
export class OCPPLocalListItemService extends AbstractService<OCPPLocalListItem>{

  constructor(
    protected http : HttpClient
    ) {
    super(http,environment.apiUrl+"/api/OCPPLocalListItem/");
  }

  baseUrl = environment.apiUrl+"/api/OCPPLocalListItem/";


}
