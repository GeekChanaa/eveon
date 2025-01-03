import { Injectable } from '@angular/core';
import { SystemReport } from 'src/_models/system-report';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class SystemReportService extends AbstractService<SystemReport>{
  baseUrl = environment.apiUrl+"/api/SystemReport/";

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/SystemReport/");
  }

}
