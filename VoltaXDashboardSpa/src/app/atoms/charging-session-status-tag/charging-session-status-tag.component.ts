import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-charging-session-status-tag',
  templateUrl: './charging-session-status-tag.component.html',
  styleUrls: ['./charging-session-status-tag.component.sass']
})
export class ChargingSessionStatusTagComponent implements OnInit {

  @Input() chargingSessionStatus : string = "";

  constructor() { }

  ngOnInit() {
  }

}
