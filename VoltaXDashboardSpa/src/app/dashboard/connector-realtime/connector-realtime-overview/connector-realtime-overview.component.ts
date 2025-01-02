import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-connector-realtime-overview',
  templateUrl: './connector-realtime-overview.component.html',
  styleUrls: ['./connector-realtime-overview.component.sass']
})
export class ConnectorRealtimeOverviewComponent implements OnInit {

  @Input() ChargePoint : any = {}

  countChargingSessions : number = 0;
  countChargingSessionsLastWeek : number = 0;

  constructor(
  ) { }

  ngOnInit() {

  }

  getStatistics(){

  }


}
