import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Connector } from 'src/_models/connector';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-charge-point-connectors',
  templateUrl: './charge-point-connectors.component.html',
  styleUrls: ['./charge-point-connectors.component.sass']
})
export class ChargePointConnectorsComponent implements OnInit {

  @Input() chargePointID : number = 0;
  connectors : any[] = [];
  cpfShow : Boolean = false;
  isConnectorVisible : boolean = false;
  displayedConnectorID : number = 0;

  constructor(
    private _connectorService: ConnectorService
  ) { 
    
  }

  ngOnInit() {
    this.getChargePointConnectors();
  }

  
  getChargePointConnectors(){
    this._connectorService.getChargePointConnectors(this.chargePointID).subscribe((data) => {
      console.log("this is the data");
      console.log(data);
      this.connectors = data;
      this.cpfShow = false;
    })
  }

  deleteChargePoint(id : number){
    this._connectorService.deleteById(id).subscribe((data) => {
      this.getChargePointConnectors();
    });
  }

  refresh(){
    this.getChargePointConnectors();
    this.isConnectorVisible = false;  
  }

  showConnector(id : number){
    this.isConnectorVisible = true;
    this.displayedConnectorID = id;
  }


}
