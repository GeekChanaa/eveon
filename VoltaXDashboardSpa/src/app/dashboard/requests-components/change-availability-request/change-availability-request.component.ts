import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Action } from 'rxjs/internal/scheduler/Action';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-change-availability-request',
  templateUrl: './change-availability-request.component.html',
  styleUrls: ['./change-availability-request.component.sass']
})
export class ChangeAvailabilityRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {
    operationalStatus : "Operative"
  } ;
  connectors : any[] = [];
  isLoading : boolean = false;

  constructor(
    private _connectorService: ConnectorService,
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
    this.getChargePointConnectors();
  }

  getChargePointConnectors(){
    this._connectorService.getChargePointConnectors(this.cpID).subscribe((data) => {
      this.request.evse = {};
      this.connectors = data;
      this.request.evse.id = this.connectors[0].connectorID;
      this.request.evse.connectorId = this.connectors[0].evseID;
    })
  }

  onSelectConnector(event : any){
    this.request.evse = {};
    let connector = JSON.parse(event.target.value);
    this.request.evse.id = connector.connectorID;
    this.request.evse.connectorId = connector.evseID;
    console.log(this.request);
  }

  changeAvailabilityRequest(){
    this.isLoading = true;
    this._configurationService.changeAvailability(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

}
