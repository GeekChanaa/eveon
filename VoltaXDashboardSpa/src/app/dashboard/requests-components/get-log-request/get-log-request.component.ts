import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { OcppReportingService } from 'src/_services/ocpp-services/ocpp-reporting.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-get-log-request',
  templateUrl: './get-log-request.component.html',
  styleUrls: ['./get-log-request.component.sass']
})
export class GetLogRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {
    id : [],
    log : {},
    logType : "DiagnosticsLog"
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _reportingServie: OcppReportingService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  getLogRequest(){
    this.isLoading = true;
    this._reportingServie.getLog(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

}
