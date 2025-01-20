import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';

import { SystemReportComment } from 'src/_models/system-report-comment';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class SystemReportCommentService extends AbstractService<SystemReportComment>{
  baseUrl = environment.apiUrl+"/api/SystemReportComment/";

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/SystemReportComment/");
  }

  // Get System Report Comments
  getSystemReportComments(id : number,page?: number, itemsPerPage?: number, itemParams?: any){
    return super.getAll(page,itemsPerPage,itemParams,"getSystemReportComments/"+id);
  }
}
