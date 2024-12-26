import { Component, Input, OnInit } from '@angular/core';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';

@Component({
  selector: 'app-request-refresh-charger-components',
  templateUrl: './request-refresh-charger-components.component.html',
  styleUrls: ['./request-refresh-charger-components.component.sass']
})
export class RequestRefreshChargerComponentsComponent implements OnInit {
  @Input() chargePointID : string = "";

  constructor(
    private _configurationService : OcppConfigurationService
  ) { }

  ngOnInit() {
  }

  refreshConnectors(){
    this._configurationService.refreshConnectors(this.chargePointID).subscribe(data => {
      console.log("this is the data after success");
      console.log(data);
    })
  }

}
