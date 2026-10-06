import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-reset-request',
  templateUrl: './reset-request.component.html',
  styleUrls: ['./reset-request.component.sass']
})
export class ResetRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Input() cpID! : number;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  isLoading : boolean = false;

  evseIDs : number[] =[]
  selectedConnector: any = {};
  request : any = {};
  
  constructor(
    private _connectorService : ConnectorService,
    private _modalService : ActionModalService,
    private _configurationService : OcppConfigurationService
  ) { }

  ngOnInit() {
    this.getEvseIds();
  }


  onConnectorChange() {
    if (this.selectedConnector) {
      let conn = JSON.parse(this.selectedConnector);
      this.request.evseId = conn.connectorID;
    }
  }

  resetRequest(){
    console.log(this.request);
    this.isLoading = true;
    this._configurationService.reset(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

  getEvseIds(){
    this._connectorService.getChargePointEvsesIds(this.cpID).subscribe((data) => {
      this.evseIDs = data;
    })
  }

}
