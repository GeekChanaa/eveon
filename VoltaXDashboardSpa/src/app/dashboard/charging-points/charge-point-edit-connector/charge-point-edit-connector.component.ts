import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
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
  loading = true;
  loadError = false;
  saving = false;
  saveError = '';
  readonly fields = [
    { key: 'connectorID', label: 'Connector ID' }, { key: 'evseID', label: 'EVSE ID' },
    { key: 'speed', label: 'Speed (kW)' }, { key: 'flatFee', label: 'Flat fee' },
    { key: 'pricePerKWh', label: 'Price per kWh' }, { key: 'pricePerMinute', label: 'Price per minute' },
    { key: 'pricePerIdleMinute', label: 'Price per idle minute' }, { key: 'pricePerHour', label: 'Price per hour' },
    { key: 'costPerKwh', label: 'Cost per kWh' }
  ];
  readonly speedOptions = [
    { value: 7.3, label: '7.3 kW' },
    { value: 11, label: '11 kW' },
    { value: 22, label: '22 kW' },
    { value: 60, label: '60 kW' },
    { value: 150, label: '150 kW' },
    { value: 300, label: '300 kW' }
  ];
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();

  constructor(
    private _chargePointService: ChargePointService,
    private _modalService:  ActionModalService,
    private _connectorService : ConnectorService
  ) {
      const controls: { [key: string]: FormControl } = {};
      this.fields.forEach(field => controls[field.key] = new FormControl(null,
        field.key === 'connectorID' || field.key === 'evseID'
          ? [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]
          : [Validators.required, Validators.min(0)]));
      this.connectorForm = new FormGroup(controls);
   }

  ngOnInit() {
    this.getConnector();
  }

  getConnector(){
    this.loading = true;
    this.loadError = false;
    this._connectorService.getById(this.connectorID).subscribe({
      next: data => {
        this.connector = data;
        const values: any = {};
        this.fields.forEach(field => values[field.key] = data[field.key] ?? null);
        this.connectorForm.reset(values);
        this.loading = false;
      },
      error: () => { this.loading = false; this.loadError = true; }
    });
  }

  cpfOnSubmit(){
    if (this.saving || this.loading || this.loadError) return;
    this.connectorForm.markAllAsTouched();
    if (this.connectorForm.invalid) return;
    const model = { ...this.connector, ...this.connectorForm.value };
    this.saving = true;
    this.saveError = '';
    this.connectorForm.disable();
    this._connectorService.edit(this.connector.id, model).subscribe({
      next: () => {
        this.saving = false;
        this.connectorForm.enable();
        this._modalService.popup(ActionModalStatusEnum.Success, 'Success', 'Connector updated successfully', 4000);
        this.successEvent.emit();
      },
      error: () => {
        this.saving = false;
        this.connectorForm.enable();
        this.saveError = 'Could not save these changes. Please try again.';
      }
    });
  }

  getControl(name: string): FormControl {
    return this.connectorForm.get(name) as FormControl;
  }

}
