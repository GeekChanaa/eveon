import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Report } from 'src/_models/report';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';


@Injectable({
  providedIn: 'root'
})
export class ReportService extends AbstractService<Report>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/Report/");
  }

  baseUrl = environment.apiUrl+"/api/Report/";

  createReport(report : any){
    return this._http.post<any>(this.baseUrl+"CreateReport",report);
  }

  getReportByID(reportID : number){
    return this._http.get<any>(this.baseUrl+"getReportByID/"+reportID);
  }

}
