import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { OcppMonitoringService } from 'src/_services/ocpp-services/ocpp-monitoring.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-clear-variable-monitoring-request',
  templateUrl: './clear-variable-monitoring-request.component.html',
  styleUrls: ['./clear-variable-monitoring-request.component.sass']
})
export class ClearVariableMonitoringRequestComponent implements OnInit {
  
  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {
    id : []
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _monitoringService: OcppMonitoringService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  clearVariableMonitoringRequest(){
    this.isLoading = true;
    this._monitoringService.clearVariableMonitoring(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

  addMonitorID(){
    if(this.monitorIDInvalid()) return;
    this.request.id.push(this.monitorID);
    this.monitorID = 0;
  }

  monitorIDInvalid(){
    return this.monitorID == 0 || this.request.id.includes(this.monitorID);
  }

  deleteMonitorID(monitorID : number){
    this.request.id = this.request.id.filter((num: number) => num !== monitorID);
  }

  isInvalidForm(){
    return this.request.id.length < 1
  }
}
