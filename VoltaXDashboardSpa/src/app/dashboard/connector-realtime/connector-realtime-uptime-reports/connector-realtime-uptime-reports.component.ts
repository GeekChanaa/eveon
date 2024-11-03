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
  cpUptimeLogs : any[] = [];

  constructor(
    private _chargePointUptimeService : ChargePointUptimeService,
    private _connectorUptimeService : ConnectorUptimeService,
    private _connectorService : ConnectorService
  ) { }

  ngOnInit() {
    this.getChargePointConnectors();
    this.getChargePointUptimeLogs();
  }

  getChargePointConnectors(){
    this._connectorService.getChargePointConnectors(this.chargePointID).subscribe((data) => {
      this.connectors = data;
    })
  }

  getChargePointUptimeLogs(){
    this._chargePointUptimeService.getChargePointUptime(this.chargePointID).subscribe((data) => {
      if(data.result)
        this.cpUptimeLogs = data.result
      console.log("uptime logs cp : ")
      console.log(data.result);
    })
  }

}
