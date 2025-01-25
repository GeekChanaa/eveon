import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-get-report-request',
  templateUrl: './get-report-request.component.html',
  styleUrls: ['./get-report-request.component.sass']
})
export class GetReportRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
