import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Action } from 'rxjs/internal/scheduler/Action';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';

@Component({
  selector: 'app-change-availability-request',
  templateUrl: './change-availability-request.component.html',
  styleUrls: ['./change-availability-request.component.sass']
})
export class ChangeAvailabilityRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {} ;
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
      this.connectors = data;
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
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }

}
