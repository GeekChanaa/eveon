import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { DownloadRequestStatusEnum } from 'src/_models/_enums/download-request-status-enum';
import { UserInfoDownloadRequest } from 'src/_models/user-info-download-request';
import { UserInfoDownloadRequestService } from 'src/_services/user-info-download-request.service';

@Component({
  selector: 'app-user-info-download-requests-list',
  templateUrl: './user-info-download-requests-list.component.html',
  styleUrls: ['./user-info-download-requests-list.component.sass']
})
export class UserInfoDownloadRequestsListComponent implements OnInit {


    userInfoDownloadRequest: UserInfoDownloadRequest = {
      id: 0,
      userID: 0,
      requestTime: new Date(),
      status: DownloadRequestStatusEnum.Pending
    };
  
    fields: string[] = [];
    filters : any = {
      status:"",
    };
  
    // Constructor
    constructor(
      private _userInfoDownloadRequestService: UserInfoDownloadRequestService,
    ) { }
  
    ngOnInit() {
      this._getItemFields();
    }
  
    getUserInfoDownloadRequests = (currentPage:  number | undefined, itemsPerPage : number | undefined, itemParams : any) => this._userInfoDownloadRequestService.getAll(currentPage, itemsPerPage, itemParams);
    updateSystemReportObservable = (id : number, model : any) => this._userInfoDownloadRequestService.edit(id, model);
  
    private _getItemFields() {
      if (!this.userInfoDownloadRequest || this.userInfoDownloadRequest == undefined) {
        return;
      }
      Object.keys(this.userInfoDownloadRequest ?? {}).forEach((element: string) => {
        if (typeof this.userInfoDownloadRequest?.[element] == "object" && this.userInfoDownloadRequest?.[element] != null && this.userInfoDownloadRequest?.[element].constructor.name == "Date")
          this.fields.push(element);
        if (typeof this.userInfoDownloadRequest?.[element] != "object") this.fields.push(element);
      });
    }
    
  
    resetFilters(){
      this.filters = {
        status : ""
      }
    }
  
}
