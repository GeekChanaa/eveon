import { Component, Input, OnInit } from '@angular/core';
import { ChargePointUptimeService } from 'src/_services/charge-point-uptime.service';
import { ConnectorUptimeService } from 'src/_services/connector-uptime.service';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-connector-realtime-uptime-reports',
  templateUrl: './connector-realtime-uptime-reports.component.html',
  styleUrls: ['./connector-realtime-uptime-reports.component.sass']
})
export class ConnectorRealtimeUptimeReportsComponent implements OnInit {

  @Input() chargePointID : number = 0;
  connectors : any[] = [];
  uptimeLogs : any[] = [];

  isLoading  : boolean = false;

  report : string = "chargePoint";

  constructor(
    private _chargePointUptimeService : ChargePointUptimeService,
    private _connectorUptimeService : ConnectorUptimeService,
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    this.getChargePointConnectors();
  }

  getChargePointConnectors(){
    this._connectorService.getChargePointConnectors(this.chargePointID).subscribe((data) => {
      this.connectors = data;
      console.log(this.connectors);
      if(this.connectors != null && this.connectors.length > 0)
        this.changedReport(this.connectors[0].id)
    })
  }

  changedReport(connectorID : any){
    this.isLoading = true;
    this._connectorUptimeService.getConnectorUptime(connectorID).subscribe((data) => {
      this.isLoading = false;
      if(data.result != null)
        this.uptimeLogs = data.result;
    })
  }

}
