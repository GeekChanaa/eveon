import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-set-monitoring-base-request',
  templateUrl: './set-monitoring-base-request.component.html',
  styleUrls: ['./set-monitoring-base-request.component.sass']
})
export class SetMonitoringBaseRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
