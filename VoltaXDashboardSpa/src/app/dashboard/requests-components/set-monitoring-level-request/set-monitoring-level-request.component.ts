import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppMonitoringService } from 'src/_services/ocpp-services/ocpp-monitoring.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-set-monitoring-level-request',
  templateUrl: './set-monitoring-level-request.component.html',
  styleUrls: ['./set-monitoring-level-request.component.sass']
})
export class SetMonitoringLevelRequestComponent implements OnInit {

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

  setMonitoringLevel(){
    this.isLoading = true;
    this._monitoringService.setMonitoringLevel(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

}
