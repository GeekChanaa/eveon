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
  loading = true;
  loadError = false;
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
    this.loading = true;
    this.loadError = false;
    this._connectorService.getChargePointConnectors(this.chargePointID).subscribe({
      next: data => { this.connectors = data; this.cpfShow = false; this.loading = false; },
      error: () => { this.loading = false; this.loadError = true; }
    });
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
