import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { GlobalConfigurations } from 'src/_models/global-configurations';


@Injectable({
  providedIn: 'root'
})
export class ConfigurationService {

  constructor(protected http : HttpClient) {
  }

  baseUrl = environment.apiUrl+"/api/Configuration/";

  getGlobalConfigurations(){
    return this.http.get<GlobalConfigurations>(this.baseUrl+"GetGlobalConfigurations");
  }

}
