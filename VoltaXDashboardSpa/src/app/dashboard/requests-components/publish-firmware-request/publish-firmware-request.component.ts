import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-publish-firmware-request',
  templateUrl: './publish-firmware-request.component.html',
  styleUrls: ['./publish-firmware-request.component.sass']
})
export class PublishFirmwareRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
