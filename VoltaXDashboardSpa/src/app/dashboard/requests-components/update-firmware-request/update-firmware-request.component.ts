import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-update-firmware-request',
  templateUrl: './update-firmware-request.component.html',
  styleUrls: ['./update-firmware-request.component.sass']
})
export class UpdateFirmwareRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
