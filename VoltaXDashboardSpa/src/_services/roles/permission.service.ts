import { Injectable } from '@angular/core';
import { Permission } from 'src/_models/permission';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';
import { AbstractService } from '../abstract-service';

@Injectable({
  providedIn: 'root'
})
export class PermissionService extends AbstractService<Permission>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/Permission/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/Permission/";


}
