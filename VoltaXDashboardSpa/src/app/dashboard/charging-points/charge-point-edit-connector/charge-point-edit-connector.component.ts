import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ConnectorService } from 'src/_services/connector.service';

@Component({
  selector: 'app-charge-point-edit-connector',
  templateUrl: './charge-point-edit-connector.component.html',
  styleUrls: ['./charge-point-edit-connector.component.sass']
})
export class ChargePointEditConnectorComponent implements OnInit {

  @Input() connectorID : number = 0;

  connectorForm : FormGroup;
  connector : any = {};
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
          pricePerHour : new FormControl(""),
          connectorID : new FormControl(""),
          evseID : new FormControl(""),
          flatFee : new FormControl(""),
      })
   }

  ngOnInit() {
    this.getConnector();
  }

  getConnector(){
    this._connectorService.getById(this.connectorID).subscribe((data) => {
      this.connector = data;  
      this.connectorForm.patchValue({
        speed: data.speed || '7.3', // Default to '7.3' if not provided
        pricePerKWh: data.pricePerKWh || '',
        pricePerMinute: data.pricePerMinute || '',
        pricePerHour: data.pricePerHour || '',
        connectorID: data.connectorID || '',
        evseID: data.evseID || '',
        flatFee: data.flatFee || '',
      });
    })
  }

  cpfOnSubmit(){
    var cpf = this.connectorForm.value; 
    this.connector = {
      ...this.connector,
      speed: cpf.speed,
      pricePerKWh: cpf.pricePerKWh,
      pricePerMinute: cpf.pricePerMinute,
      pricePerHour: cpf.pricePerHour,
      connectorID: cpf.connectorID,
      evseID: cpf.evseID,
      flatFee: cpf.flatFee,
    };
    this._connectorService.edit(this.connector.id,this.connector).subscribe((data) => {
      this._modalService.popup(ActionModalStatusEnum.Success, "Success", "The Connector has been edited succesfully", 4000);
      this.successEvent.emit();
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error, "Error", "Something Went Wrong", 4000);
    })
  }

  getControl(name: string): FormControl {
    return this.connectorForm.get(name) as FormControl;
  }

}
