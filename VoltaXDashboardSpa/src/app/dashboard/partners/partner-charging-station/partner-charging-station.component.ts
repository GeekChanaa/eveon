import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-partner-charging-station',
  templateUrl: './partner-charging-station.component.html',
  styleUrls: ['./partner-charging-station.component.sass']
})
export class PartnerChargingStationComponent implements OnInit {

  @Input() chargingStationID : number = 0

  constructor() { }

  ngOnInit() {
  }

}
