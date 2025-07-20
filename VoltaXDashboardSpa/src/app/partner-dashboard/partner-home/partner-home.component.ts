import { Component, OnInit } from '@angular/core';
import { AuthService } from 'src/_services/auth.service';
import { ConnectorStatusService } from 'src/_services/connector-status.service';
import { PartnerConnectorStatusService } from 'src/_services/partner-services/partner-connector-status.service';

@Component({
  selector: 'app-partner-home',
  templateUrl: './partner-home.component.html',
  styleUrls: ['./partner-home.component.sass']
})
export class PartnerHomeComponent implements OnInit {

  constructor(
  ) { }

  ngOnInit() {
  }


}
