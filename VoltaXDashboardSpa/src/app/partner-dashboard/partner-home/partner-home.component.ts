import { Component, OnInit } from '@angular/core';
import { ConnectorStatusService } from 'src/_services/connector-status.service';

@Component({
  selector: 'app-partner-home',
  templateUrl: './partner-home.component.html',
  styleUrls: ['./partner-home.component.css']
})
export class PartnerHomeComponent implements OnInit {

  // numbers
  availableConnectors : number = 0;
  unavailableConnectors : number = 0;
  faultedConnectors : number = 0;
  occupiedConnectors : number = 0;
  reservedConnectors : number = 0;

  // Constructor
  constructor(
    private _connectorStatusService : ConnectorStatusService
  ) { }

  // On init cycle hook 
  ngOnInit() {
    this.getConnectorStatusNumbers();
  }
  

  // Getting connector Status numbers
  getConnectorStatusNumbers(){
    this._connectorStatusService.getNumberOfConnectorsByStatus("Available").subscribe((data) => this.availableConnectors = data);
    this._connectorStatusService.getNumberOfConnectorsByStatus("Unavailable").subscribe((data) => this.unavailableConnectors = data);
    this._connectorStatusService.getNumberOfConnectorsByStatus("Faulted").subscribe((data) => this.faultedConnectors = data);
    this._connectorStatusService.getNumberOfConnectorsByStatus("Occupied").subscribe((data) => this.occupiedConnectors = data);
    this._connectorStatusService.getNumberOfConnectorsByStatus("Reserved").subscribe((data) => this.reservedConnectors = data);
  }

}
