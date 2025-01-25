import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';

@Component({
  selector: 'app-clear-display-message-request',
  templateUrl: './clear-display-message-request.component.html',
  styleUrls: ['./clear-display-message-request.component.sass']
})
export class ClearDisplayMessageRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {} ;
  connectors : any[] = [];
  isLoading : boolean = false;

  constructor(
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  clearDisplayMessageRequest(){
    this.isLoading = true;
    this._configurationService.clearDisplayMessage(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }

}
