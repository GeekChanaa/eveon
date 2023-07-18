import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Connector } from 'src/_models/connector';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-charge-point-connectors',
  templateUrl: './charge-point-connectors.component.html',
  styleUrls: ['./charge-point-connectors.component.css']
})
export class ChargePointConnectorsComponent implements OnInit {

  @Input() connectors : Connector[] = [];
  @Input() chargePointID : number = 0;
  connector : any = {};
  connectorSpeed : number = 0;
  connectorPower : number = 0;
  connectorID : number = 0;
  @Output() deleteConnectorEvent : EventEmitter<number> = new EventEmitter<number>();
  @Input() reloadChargePoint : EventEmitter<void> = new EventEmitter();
  showcf : boolean = false;

  constructor(
    private _connectorService : ConnectorService
  ) { }

  ngOnInit(
  ) {
  }

  deleteConnector(id : number){
    this.deleteConnectorEvent.emit(id);
  }

  // create Connector : 
  createConnector(){
    this.connector.connectorType = "Type2";
    this.connector.chargePointID = this.chargePointID;
    this.connector.connectorID = this.connectorID;
    this.connector.speed = this.connectorSpeed;
    this.connector.power = this.connectorPower;
    this._connectorService.create(this.connector).subscribe((data) => {
      this.showcf = false;
      this.reloadChargePoint.emit();
    })
  }

  

}
