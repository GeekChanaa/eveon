import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-install-certificate-request',
  templateUrl: './install-certificate-request.component.html',
  styleUrls: ['./install-certificate-request.component.sass']
})
export class InstallCertificateRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  constructor() { }

  ngOnInit() {
  }

}
