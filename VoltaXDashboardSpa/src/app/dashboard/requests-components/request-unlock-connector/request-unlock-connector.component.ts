import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';

@Component({
  selector: 'app-request-unlock-connector',
  templateUrl: './request-unlock-connector.component.html',
  styleUrls: ['./request-unlock-connector.component.sass']
})
export class RequestUnlockConnectorComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {} ;
  connectors : any[] = [];
  isLoading : boolean = false;

  constructor(
    private _connectorService: ConnectorService,
    private _evDriverService: OcppEvDriverService,
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

  unlockConnectorRequest(){
    this.isLoading = true;
    this._evDriverService.unlockConnector(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Success,"Success !", "Request Sent successfully ! ",4000);
      this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      this._modalService.popup(ActionModalStatusEnum.Error, "Error !","Something went wrong please contact your system administrator",4000);
    })
  }

}
