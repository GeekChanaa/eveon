import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { PartnerConnectorStatusService } from 'src/_services/partner-services/partner-connector-status.service';

@Component({
  selector: 'app-partner-home-connector-statuses',
  templateUrl: './partner-home-connector-statuses.component.html',
  styleUrls: ['./partner-home-connector-statuses.component.sass']
})
export class PartnerHomeConnectorStatusesComponent implements OnInit {
  
  availableConnectors : number = 0;
  unavailableConnectors : number = 0;
  faultedConnectors : number = 0;
  occupiedConnectors : number = 0;
  disconnectedConnectors : number = 0;
  partnerID : number = 0;
  
  constructor(
    private _partnerConnectorStatusService : PartnerConnectorStatusService,
    private _authService: AuthService
  ) { }

  ngOnInit() {
    var user = this._authService.decodedToken;
    this.partnerID = parseInt(user.partnerID);
    this.getConnectorStatusNumbers();
  }

  getConnectorStatusNumbers(){
    this._partnerConnectorStatusService.getNumberOfPartnerConnectorsByAllStatus(this.partnerID).subscribe((data) => {
      this.availableConnectors = data.nbrAvailableConnectors;
      this.unavailableConnectors = data.nbrUnavailableConnectors;
      this.faultedConnectors = data.nbrFaultedConnectors;
      this.occupiedConnectors = data.nbrOccupiedConnectors;
      this.disconnectedConnectors = data.nbrDisconnectedConnectors;	

      console.log("this is the data");
      console.log(data);
    });
  }
}
