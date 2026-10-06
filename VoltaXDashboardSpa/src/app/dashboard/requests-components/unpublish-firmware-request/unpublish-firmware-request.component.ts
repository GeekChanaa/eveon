import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-unpublish-firmware-request',
  templateUrl: './unpublish-firmware-request.component.html',
  styleUrls: ['./unpublish-firmware-request.component.sass']
})
export class UnpublishFirmwareRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  request : any = {
    
  } ;
  isLoading : boolean = false;

  monitorID! : number;

  constructor(
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService
  ) { }

  ngOnInit() {
  }

  unpublishFirmware(){
    this.isLoading = true;
    this._configurationService.unpublishFirmware(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }
}
