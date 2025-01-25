import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-set-network-profile-request',
  templateUrl: './set-network-profile-request.component.html',
  styleUrls: ['./set-network-profile-request.component.sass']
})
export class SetNetworkProfileRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
