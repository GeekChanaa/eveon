import { Component, Input, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-request-refresh-charger-components',
  templateUrl: './request-refresh-charger-components.component.html',
  styleUrls: ['./request-refresh-charger-components.component.sass']
})
export class RequestRefreshChargerComponentsComponent implements OnInit {
  @Input() chargePointID : string = "";

  constructor(
    private _configurationService : OcppConfigurationService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
  }

  refreshConnectors(){
    this._configurationService.refreshConnectors(this.chargePointID).subscribe(data => {
      showOcppCommandFeedback(this._modalService, data);
    },(error) => {
      showOcppCommandFeedback(this._modalService, error);
    })
  }

}
