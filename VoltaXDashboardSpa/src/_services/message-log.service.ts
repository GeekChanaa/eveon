import { Injectable } from '@angular/core';
import { MessageLog } from 'src/_models/message-log';
import { AbstractService } from './abstract-service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

import { Observable } from 'rxjs';
import { PaginatedResult } from 'src/_models/pagination';

@Injectable({
  providedIn: 'root'
})
export class MessageLogService extends AbstractService<MessageLog>{

  constructor(protected http : HttpClient) {
    super(http,environment.apiUrl+"/api/MessageLog/");
  }

  baseUrl = environment.apiUrl+"/api/MessageLog/";

  getChargePointMessageLogs(chargePointID : string,page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<any[]>>{
    return super.getAll(page,itemsPerPage,itemParams,"getChargePointMessageLogs/"+chargePointID);
  }

}
