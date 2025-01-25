import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-set-monitoring-level-request',
  templateUrl: './set-monitoring-level-request.component.html',
  styleUrls: ['./set-monitoring-level-request.component.sass']
})
export class SetMonitoringLevelRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
