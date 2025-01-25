import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-reset-request',
  templateUrl: './reset-request.component.html',
  styleUrls: ['./reset-request.component.sass']
})
export class ResetRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
