import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-set-variable-monitoring-request',
  templateUrl: './set-variable-monitoring-request.component.html',
  styleUrls: ['./set-variable-monitoring-request.component.sass']
})
export class SetVariableMonitoringRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
