import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ConnectorService } from 'src/_services/connector.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-charge-point-connector',
  templateUrl: './charge-point-connector.component.html',
  styleUrls: ['./charge-point-connector.component.sass']
})
export class ChargePointConnectorComponent implements OnInit {

  @Output() refreshEvent : EventEmitter<number> = new EventEmitter<number>();
  @Input() connectorID : number = 0;

  editingConnector : number = 0;

  chargePointCategory : string = "";
  chargePointStatus : string = "";

  @Input() connector : any = {};

  constructor(
    private _connectorService : ConnectorService,
    private _enumMappings : EnumMappingService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    this.chargePointCategory = this._enumMappings.getEnumMapping("ChargePointCategory")[this.connector.category];
    this.getConnector();
  }

  deleteConnector(id : number){
    this._connectorService.deleteById(this.connectorID).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Deleted", "The Connector : "+ this.connector.connectorID + " was deleted succesfully", 4000);
      this.refreshEvent.emit();
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something went wrong",4000);
      this.refreshEvent.emit();
    })
  }

  getConnector(){
    this._connectorService.getById(this.connectorID).subscribe((data) => {
      this.connector = data;
    })
  }

  editConnector(id : number){
    this.editingConnector = id;
  }


}
