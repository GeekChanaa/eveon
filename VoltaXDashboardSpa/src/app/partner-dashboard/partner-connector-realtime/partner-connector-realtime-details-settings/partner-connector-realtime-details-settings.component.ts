import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-partner-connector-realtime-details-settings',
  templateUrl: './partner-connector-realtime-details-settings.component.html',
  styleUrls: ['./partner-connector-realtime-details-settings.component.sass']
})
export class PartnerConnectorRealtimeDetailsSettingsComponent implements OnInit {
  
  @Input() chargePoint : any = {};

  constructor(
  ) { }

  ngOnInit() {
  }

}
