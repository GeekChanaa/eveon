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

  constructor(
    private _requestService: UserInfoDownloadRequestService,
    private _route: ActivatedRoute,
    private _modalService : ActionModalService
  ) {
   }

  ngOnInit() {
    var idParam = this._route.snapshot.paramMap.get('id')
    if (idParam != null) {
      var id = parseInt(idParam);
      this.getRequestByID(id);
    }
  }


  getRequestByID(id : number){
    this.requestID = id;
    this._requestService.getRequestByID(id).subscribe((cs) => {
      this.request = cs;
      this.requestLoaded = true;
    })
  }

  approveRequest(){
    this._requestService.approveRequest(this.requestID).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"User Created !","User Created Successfully !",4000);
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }

  denyRequest(){
    this._requestService.denyRequest(this.requestID).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success,"User Created !","User Created Successfully !",4000);
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }
}
