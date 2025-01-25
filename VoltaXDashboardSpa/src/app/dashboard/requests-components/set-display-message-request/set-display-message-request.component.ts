import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-set-display-message-request',
  templateUrl: './set-display-message-request.component.html',
  styleUrls: ['./set-display-message-request.component.sass']
})
export class SetDisplayMessageRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
