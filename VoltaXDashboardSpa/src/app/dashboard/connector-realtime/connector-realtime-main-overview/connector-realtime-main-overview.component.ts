import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-connector-realtime-main-overview',
  templateUrl: './connector-realtime-main-overview.component.html',
  styleUrls: ['./connector-realtime-main-overview.component.sass']
})
export class ConnectorRealtimeMainOverviewComponent implements OnInit {

  @Input() chargePoint : any = {}

  constructor() { }

  ngOnInit() {
  }

}
