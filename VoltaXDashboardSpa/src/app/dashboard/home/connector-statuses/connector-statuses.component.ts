import { Component, OnInit } from '@angular/core';
import { ConnectorStatusService } from 'src/_services/connector-status.service';

@Component({
  selector: 'app-connector-statuses',
  templateUrl: './connector-statuses.component.html',
  styleUrls: ['./connector-statuses.component.sass']
})
export class ConnectorStatusesComponent implements OnInit {

  availableConnectors : number = 0;
  unavailableConnectors : number = 0;
  faultedConnectors : number = 0;
  occupiedConnectors : number = 0;
  disconnectedConnectors : number = 0;
  
  constructor(
    private _connectorStatusService : ConnectorStatusService
  ) { }

  ngOnInit() {
    this.getConnectorStatusNumbers();
  }

  getConnectorStatusNumbers(){
    this._connectorStatusService.getNumberOfConnectorsByAllStatus().subscribe((data) => {
      this.availableConnectors = data.nbrAvailableConnectors;
      this.unavailableConnectors = data.nbrUnavailableConnectors;
      this.faultedConnectors = data.nbrFaultedConnectors;
      this.occupiedConnectors = data.nbrOccupiedConnectors;
      this.disconnectedConnectors = data.nbrDisconnectedConnectors;	

      console.log(this.faultedConnectors);
    });
  }

}
