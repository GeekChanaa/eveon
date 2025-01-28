import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppComponentsService } from 'src/_services/ocpp-components.service';
import { OcppMonitoringService } from 'src/_services/ocpp-services/ocpp-monitoring.service';

@Component({
  selector: 'app-set-monitoring-base-request',
  templateUrl: './set-monitoring-base-request.component.html',
  styleUrls: ['./set-monitoring-base-request.component.sass']
})
export class SetMonitoringBaseRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;

  request : any = {} ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _monitoringService: OcppMonitoringService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  setMonitoringBaseRequest(){
    this.isLoading = true;
    this._monitoringService.setMonitoringBase(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }



}
