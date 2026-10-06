import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { OcppConfigurationService } from 'src/_services/ocpp-services/ocpp-configuration.service';
import { OcppEvDriverService } from 'src/_services/ocpp-services/ocpp-ev-driver.service';
import { showOcppCommandFeedback } from 'src/_services/ocpp-services/ocpp-command-feedback';

@Component({
  selector: 'app-clear-display-message-request',
  templateUrl: './clear-display-message-request.component.html',
  styleUrls: ['./clear-display-message-request.component.sass']
})
export class ClearDisplayMessageRequestComponent implements OnInit {

  @Input() chargePointID! : string;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Input() cpID! : number;
  request : any = {} ;
  connectors : any[] = [];
  isLoading : boolean = false;

  requestForm : FormGroup;
  

  constructor(
    private _configurationService: OcppConfigurationService,
    private _modalService: ActionModalService
  ) { 
    this.requestForm = new FormGroup({
      id: new FormControl('', Validators.required)
    })
  }

  ngOnInit() {
  }

  clearDisplayMessageRequest(){
    this.request = this.requestForm.value;
    this.isLoading = true;
    this._configurationService.clearDisplayMessage(this.chargePointID, this.request).subscribe((data) => {
      this.isLoading = false;
      if(showOcppCommandFeedback(this._modalService, data)) this.successEvent.emit();
    },(error) => {
      this.isLoading = false;
      showOcppCommandFeedback(this._modalService, error);
    })
  }

  getControl(name: string): FormControl {
    return this.requestForm.get(name) as FormControl;
  }

}
