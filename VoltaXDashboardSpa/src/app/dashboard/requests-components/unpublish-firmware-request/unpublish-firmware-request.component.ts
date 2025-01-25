import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-unpublish-firmware-request',
  templateUrl: './unpublish-firmware-request.component.html',
  styleUrls: ['./unpublish-firmware-request.component.sass']
})
export class UnpublishFirmwareRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
