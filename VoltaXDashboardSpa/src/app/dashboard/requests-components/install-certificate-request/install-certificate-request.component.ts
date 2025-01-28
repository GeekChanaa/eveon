import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppSecurityService } from 'src/_services/ocpp-services/ocpp-security.service';

@Component({
  selector: 'app-install-certificate-request',
  templateUrl: './install-certificate-request.component.html',
  styleUrls: ['./install-certificate-request.component.sass']
})
export class InstallCertificateRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {} ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _securityService: OcppSecurityService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  installCertificateRequest(){
    this.isLoading = true;
    this._securityService.installCertificate(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }

}
