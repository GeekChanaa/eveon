import { animate, style, transition, trigger } from '@angular/animations';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { GlobalConfigurations } from 'src/_models/global-configurations';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ConfigurationService } from 'src/_services/configuration.service';
import { ChargingStationTypeEnum } from 'src/app/dashboard/charging-stations/create-charging-station/create-charging-station.component';

@Component({
  selector: 'app-create-charge-point-add-connectors',
  templateUrl: './create-charge-point-add-connectors.component.html',
  styleUrls: ['./create-charge-point-add-connectors.component.sass'],
  animations: [
      trigger('modalAnimation', [
        transition(':enter', [
          style({ opacity: 0, transform: 'translateY(-20px)' }),
          animate('200ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
        ]),
        transition(':leave', [
          animate('200ms ease-in', style({ opacity: 0, transform: 'translateY(-20px)' }))
        ])
      ]),
      trigger('overlayAnimation', [
        transition(':enter', [
          style({ opacity: 0 }),
          animate('200ms ease-out', style({ opacity: 1 }))
        ]),
        transition(':leave', [
          animate('200ms ease-in', style({ opacity: 0 }))
        ])
      ])
    ]
})
export class CreateChargePointAddConnectorsComponent implements OnInit {

  chargingStationType : ChargingStationTypeEnum = ChargingStationTypeEnum.VoltaXStation;
  @Input() chargePoint : any = {};

  @Output() previousStepEvent : EventEmitter<void> =  new EventEmitter();
  @Output() submit : EventEmitter<any[]> = new EventEmitter<any[]>();
    
  showConnectorModal = false;
  currentConnectorIndex: number | null = null;
  isSaving : boolean = false;
  
  configuration? : GlobalConfigurations;
  
  connectorForm: FormGroup;

  constructor(
    private _fb : FormBuilder,
    private _configurationService : ConfigurationService,
    private _chargePointService : ChargePointService,
    private _modalService: ActionModalService,
    private _router : Router
  ) { 
    this.connectorForm = this._fb.group({
      power: new FormControl('7.3', [Validators.required]),
      pricePerKWh: new FormControl('', [Validators.required]),
      pricePerIdleMinute: new FormControl('', [Validators.required]),
      pricePerMinute: new FormControl('', [Validators.required]),
      costPerKwh: new FormControl('', [Validators.required]),
      flatFee: new FormControl('', [Validators.required]),
      type: new FormControl('', [Validators.required]),
      connectorID: new FormControl('', [Validators.required]),
      evseID: new FormControl('', [Validators.required]),
    });

    if(this.chargePoint.partnerID != null) this.chargingStationType = ChargingStationTypeEnum.VoltaXStation;
    else this.chargingStationType = ChargingStationTypeEnum.PartnerStation;
  }

  ngOnInit() {
    this.chargePoint.connectors = [];
    this.getConfigurations();
  }

  getConfigurations(){
    this._configurationService.getGlobalConfigurations().subscribe((data : GlobalConfigurations) => {
      this.configuration = data;
    })
  }

  openAddConnectorModal() {
    if(this.chargingStationType == ChargingStationTypeEnum.VoltaXStation){
      this.connectorForm.reset({
        power : "22",
        pricePerKWh : this.configuration?.defaultPricePerKwh,
        pricePerIdleMinute : this.configuration?.defaultIdleTimePricing,
        pricePerMinute: this.configuration?.defaultPricePerMinute,
        costPerKwh: this.configuration?.defaultPricePerKwh,
        flatFee: this.configuration?.defaultPricePerKwh,
        type: "cType2",
        connectorID : 0,
        evseID : 0
      });
    }else{
      this.connectorForm.reset({
        power : "22",
        pricePerKWh : 0,
        pricePerIdleMinute : 0,
        pricePerMinute: 0,
        costPerKwh: 0,
        flatFee: 0,
        type : "cType2",
        connectorID : 0,
        evseID : 0
      });
    }
    
    this.currentConnectorIndex = null;
    this.showConnectorModal = true;
  }

  closeConnectorModal() {
    this.showConnectorModal = false;
  }

  previousStep = () => this.previousStepEvent.emit();

  saveConnector() {
      if (this.connectorForm.invalid) {
        this.markFormGroupTouched(this.connectorForm);
        return;
      }
      
      const formValue = this.connectorForm.value;
      const connector = {
        type : formValue.type,
        power : formValue.power,
        pricePerKWh : formValue.pricePerKWh,
        pricePerIdleMinute : formValue.pricePerIdleMinute,
        pricePerMinute : formValue.pricePerMinute,
        costPerKwh : formValue.costPerKwh,
        flatFee : formValue.flatFee,
        connectorID : formValue.connectorID,
        evseID : formValue.evseID
      };

      if(!this.chargePoint.connectors){
        this.chargePoint.connectors = [];
      }
      
      // Check if we're adding a new connector or editing an existing one
      if (this.currentConnectorIndex !== null) {
        this.chargePoint.connectors[this.currentConnectorIndex] = connector;
      } else {
        // Make sure we don't exceed connector limit based on category
        const maxConnectors = this.chargePoint.category === ChargePointCategoryEnum.Single ? 1 : 2;
        if (this.chargePoint.connectors.length >= maxConnectors) {
          alert(`A ${this.chargePoint.category} charge point can only have ${maxConnectors} connector(s).`);
          return;
        }
        this.chargePoint.connectors.push(connector);
      }
      
      this.closeConnectorModal();
    }

    markFormGroupTouched(formGroup: FormGroup) {
      Object.keys(formGroup.controls).forEach(key => {
        const control = formGroup.get(key);
        control?.markAsTouched();
        
        if (control instanceof FormGroup) {
          this.markFormGroupTouched(control);
        }
      });
    }

    

    canAddConnector(): boolean {
      const maxConnectors = this.chargePoint.category === 'Single' ? 1 : 2;
      return (this.chargePoint.connectors?.length || 0) < maxConnectors;
    }


    openEditConnectorModal( connectorIndex: number) {
      const connector = this.chargePoint.connectors[connectorIndex];
      this.connectorForm.patchValue({
        speed: connector.speed,
        pricePerKWh: connector.pricePerKWh,
        pricePerMinute: connector.pricePerMinute,
        pricePerHour: connector.pricePerHour
      });
      this.currentConnectorIndex = connectorIndex;
      this.showConnectorModal = true;
    }

    deleteConnector(connectorIndex: number) {
      if (confirm('Are you sure you want to delete this connector?')) {
        this.chargePoint.connectors.splice(connectorIndex, 1);
      }
    }


    saveInformations(){
      this.isSaving = true;
      this._chargePointService.create(this.chargePoint).subscribe((data) => {
        this._modalService.popup(ActionModalStatusEnum.Success,"Success !","Created Success", 4000);
        this.isSaving = false;
        this._router.navigateByUrl("/dashboard/charging-points")
      },(error) => {
        this._modalService.popup(ActionModalStatusEnum.Error,"Error !","Something went wrong please try again later", 4000);
      })
    }


}
