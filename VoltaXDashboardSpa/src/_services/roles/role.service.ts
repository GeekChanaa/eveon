import { Injectable } from '@angular/core';
import { Role } from 'src/_models/role';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';
import { AbstractService } from '../abstract-service';

@Injectable({
  providedIn: 'root'
})
export class RoleService extends AbstractService<Role>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/Role/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/Role/";


}
