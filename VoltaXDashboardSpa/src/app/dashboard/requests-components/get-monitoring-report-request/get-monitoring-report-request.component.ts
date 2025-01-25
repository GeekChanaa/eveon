import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-get-monitoring-report-request',
  templateUrl: './get-monitoring-report-request.component.html',
  styleUrls: ['./get-monitoring-report-request.component.sass']
})
export class GetMonitoringReportRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
