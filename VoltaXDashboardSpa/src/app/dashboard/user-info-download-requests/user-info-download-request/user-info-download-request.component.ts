import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { UserInfoDownloadRequestService } from 'src/_services/user-info-download-request.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-user-info-download-request',
  templateUrl: './user-info-download-request.component.html',
  styleUrls: ['./user-info-download-request.component.sass']
})
export class UserInfoDownloadRequestComponent implements OnInit {
  requestID : number = 0;
  requestLoaded : boolean = false;

  request: any = {};

  isLoading : boolean = false;

  constructor(
    private _requestService: UserInfoDownloadRequestService,
    private _route: ActivatedRoute,
    private _modalService : ActionModalService
  ) {
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      this.requestID = parseInt(idParam);
      this.getRequest();
    }
  }


  getRequest(){
    this._requestService.getRequestByID(this.requestID).subscribe((cs) => {
      this.request = cs;
      this.requestLoaded = true;
    })
  }

  approveRequest(){
    this.isLoading = true;
    this._requestService.approveRequest(this.requestID).subscribe((data) => {
      this.isLoading = false;
      this.getRequest();
      this._modalService.popup(ActionModalStatusEnum.Success,"Approved !","Request Approved !",4000);
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }

  denyRequest(){
    this.isLoading = true;
    this.getRequest();
    this._requestService.denyRequest(this.requestID).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Denied !","Request Denied !",4000);
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }
}
