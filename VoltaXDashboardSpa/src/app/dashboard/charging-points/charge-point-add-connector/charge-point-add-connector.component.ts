import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-charge-point-add-connector',
  templateUrl: './charge-point-add-connector.component.html',
  styleUrls: ['./charge-point-add-connector.component.sass']
})
export class ChargePointAddConnectorComponent implements OnInit {


  connectorForm : FormGroup;
  @Input() chargePointID : number = 0;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();

  constructor(
    private _chargePointService: ChargePointService,
    private _modalService:  ActionModalService,
    private _connectorService : ConnectorService
  ) {
    this.connectorForm = new FormGroup({
      speed: new FormControl('7.3'),
      pricePerKWh : new FormControl(""),
      pricePerMinute : new FormControl(""),
      pricePerIdleMinute : new FormControl(""),
      connectorID : new FormControl(""),
      evseID : new FormControl(""),
      pricePerHour : new FormControl(""),
      costPerKwh : new FormControl(""),
      flatFee : new FormControl(""),
    })
   }

  ngOnInit() {
  }

  cpfOnSubmit(){
    var cpf = this.connectorForm.value;
    const connector : any = {
      speed: cpf.speed,
      pricePerKWh: cpf.pricePerKWh,
      connectorID: cpf.connectorID,
      evseID: cpf.evseID,
      pricePerMinute: cpf.pricePerMinute,
      pricePerIdleMinute: cpf.pricePerIdleMinute,
      pricePerHour: cpf.pricePerHour,
      chargePointID: this.chargePointID,
      costPerKwh: cpf.costPerKwh,
      flatFee: cpf.flatFee, 
    }
    this._connectorService.create(connector).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "The Connector has been added succesfully", 4000);
      this.successEvent.emit();
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong", 4000);
    })
  }

  getControl(name: string): FormControl {
    return this.connectorForm.get(name) as FormControl;
  }

}
