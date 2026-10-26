import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ListFilterDef } from 'src/_models/list-filter';
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
    filterDefs: ListFilterDef[] = [
      { key: 'status', label: 'Status', type: 'multiselect', options: [{ value: 'Pending', label: 'Pending' }, { value: 'Processing', label: 'Processing' }, { value: 'Approved', label: 'Approved' }, { value: 'Denied', label: 'Denied' }, { value: 'Completed', label: 'Completed' }, { value: 'Expired', label: 'Expired' }, { value: 'Failed', label: 'Failed' }] },
      { key: 'requestTime', label: 'Requested', type: 'date-range' },
      { key: 'completedAt', label: 'Completed', type: 'date-range' },
      { key: 'downloaded', field: 'DownloadedAt', label: 'Downloaded', type: 'boolean', presence: true },
      { key: 'user', field: 'User-Email', label: 'User email', type: 'text' },
    ];
  
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
  
}
