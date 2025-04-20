import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AbstractService } from './abstract-service';
import { Observable } from 'rxjs';
import { PaginatedResult } from 'src/_models/pagination';
import { UserInfoDownloadRequest } from 'src/_models/user-info-download-request';

@Injectable({
  providedIn: 'root'
})
export class UserInfoDownloadRequestService extends AbstractService<UserInfoDownloadRequest>{

  constructor(protected http : HttpClient) {
    super(http, environment.apiUrl+"/api/UserInfoDownloadRequest/");
  }

  // Base URL for the api
  baseUrl = environment.apiUrl+"/api/UserInfoDownloadRequest/";

  getRequestByID(requestID : number){
    return this.http.get(this.baseUrl+"getRequestByID/"+requestID);
  }

  approve(requestID : number){
    return this.http.put(this.baseUrl+"approve/"+requestID,{});
  }

  deny(requestID : number){
    return this.http.put(this.baseUrl+"deny/"+requestID,{});
  }

  createRequest(userID : number){
    return this.http.post(this.baseUrl+"create/", userID);
  }

  getAllRequests(page?: number, itemsPerPage?: number, itemParams?: any, endpoint: string = ""): Observable<PaginatedResult<any[]>>{
    return super.getAll(page,itemsPerPage,itemParams,"GetAllRequests");
  }

  userLastRequest(userID : number){
    return this.http.get(this.baseUrl+"UserLastRequest/"+userID);
  }

  approveRequest(requestID : number){
    return this.http.post(this.baseUrl+"approveRequest/"+requestID , {});
  }

  denyRequest(requestID : number){
    return this.http.post(this.baseUrl+"denyRequest/"+requestID , {});
  }


}
