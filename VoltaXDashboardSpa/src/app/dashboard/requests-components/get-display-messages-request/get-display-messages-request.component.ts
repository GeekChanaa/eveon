import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { OcppMonitoringService } from 'src/_services/ocpp-services/ocpp-monitoring.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-get-display-messages-request',
  templateUrl: './get-display-messages-request.component.html',
  styleUrls: ['./get-display-messages-request.component.sass']
})
export class GetDisplayMessagesRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {
    id : []
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  getDisplayMessageRequest(){
    this.isLoading = true;
    if(this.request.id.length == 0)
      this.request.id = null;
    this._configurationService.getDisplayMessages(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

  addMonitorID(){
    if(this.monitorIDInvalid()) return;
    if(this.request.id == null) this.request.id = [];
    this.request.id.push(this.monitorID);
    this.monitorID = 0;
  }

  monitorIDInvalid(){
    return this.monitorID == 0 || this.request.id.includes(this.monitorID);
  }

  deleteMonitorID(monitorID : number){
    this.request.id = this.request.id.filter((num: number) => num !== monitorID);
  }

}
