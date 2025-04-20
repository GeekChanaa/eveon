import { animate, style, transition, trigger } from '@angular/animations';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargingStationTypeEnum } from '../create-charging-station.component';
import { ConfigurationService } from 'src/_services/configuration.service';
import { GlobalConfigurations } from 'src/_models/global-configurations';

@Component({
  selector: 'app-create-charging-station-add-charge-points',
  templateUrl: './create-charging-station-add-charge-points.component.html',
  styleUrls: ['./create-charging-station-add-charge-points.component.sass'],
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
export class CreateChargingStationAddChargePointsComponent implements OnInit {
  chargePoints: any[] = [];
  showChargePointModal = false;
  showConnectorModal = false;
  currentChargePointIndex: number | null = null;
  currentConnectorIndex: number | null = null;
  @Output() previousStepEvent : EventEmitter<void> =  new EventEmitter();
  @Output() nextStep : EventEmitter<any[]> = new EventEmitter<any[]>();
  @Input() chargingStationType : ChargingStationTypeEnum | undefined;
  configuration? : GlobalConfigurations;
  
  chargePointForm: FormGroup;
  connectorForm: FormGroup;
  
  ChargePointCategory = ChargePointCategoryEnum;
  ChargePointStatus = ChargePointStatusEnum;
  
  constructor(
      private fb: FormBuilder,
      private _configurationService : ConfigurationService) {
    this.chargePointForm = this.fb.group({
      serialNumber: new FormControl('', [Validators.required]),
      status: new FormControl("Available"),
      category: new FormControl("Single"),
    });
    
    this.connectorForm = this.fb.group({
      power: new FormControl('7.3', [Validators.required]),
      pricePerKWh: new FormControl('', [Validators.required]),
      pricePerIdleMinute: new FormControl('', [Validators.required]),
      pricePerMinute: new FormControl('', [Validators.required]),
      costPerKwh: new FormControl('', [Validators.required]),
      flatFee: new FormControl('', [Validators.required]),
      type: new FormControl('', [Validators.required]),
    });
  }

  getConfigurations(){
    this._configurationService.getGlobalConfigurations().subscribe((data : GlobalConfigurations) => {
      this.configuration = data;
      console.log(this.configuration);
    })
  }
  
  ngOnInit(): void {
    this.getConfigurations();
  }
  
  openAddChargePointModal() {
    this.chargePointForm.reset({
      status: "Available",
      category: "Single"
    });
    this.currentChargePointIndex = null;
    this.showChargePointModal = true;
  }
  
  openEditChargePointModal(index: number) {
    const chargePoint = this.chargePoints[index];
    this.chargePointForm.patchValue({
      serialNumber: chargePoint.serialNumber,
      status: chargePoint.status,
      category: chargePoint.category
    });
    this.currentChargePointIndex = index;
    this.showChargePointModal = true;
  }
  
  closeChargePointModal() {
    this.showChargePointModal = false;
  }
  
  saveChargePoint() {
    if (this.chargePointForm.invalid) {
      this.markFormGroupTouched(this.chargePointForm);
      return;
    }
    
    const formValue = this.chargePointForm.value;
    const chargePoint = {
      serialNumber: formValue.serialNumber,
      status: formValue.status,
      category: formValue.category,
      connectors: []
    };
    
    if (this.currentChargePointIndex !== null) {
      // Preserve existing connectors when editing
      chargePoint.connectors = this.chargePoints[this.currentChargePointIndex].connectors;
      this.chargePoints[this.currentChargePointIndex] = chargePoint;
    } else {
      this.chargePoints.push(chargePoint);
    }
    
    this.closeChargePointModal();
  }
  
  openAddConnectorModal(chargePointIndex: number) {
    if(this.chargingStationType == ChargingStationTypeEnum.VoltaXStation){
      this.connectorForm.reset({
        power : "22",
        pricePerKWh : this.configuration?.defaultPricePerKwh,
        pricePerIdleMinute : this.configuration?.defaultIdleTimePricing,
        pricePerMinute: this.configuration?.defaultPricePerMinute,
        costPerKwh: this.configuration?.defaultPricePerKwh,
        flatFee: this.configuration?.defaultPricePerKwh,
        type: "cType2"
      });
    }else{
      this.connectorForm.reset({
        power : "22",
        pricePerKWh : 0,
        pricePerIdleMinute : 0,
        pricePerMinute: 0,
        costPerKwh: 0,
        flatFee: 0,
        type : "cType2"
      });
    }
    
    this.currentChargePointIndex = chargePointIndex;
    this.currentConnectorIndex = null;
    this.showConnectorModal = true;
  }
  
  openEditConnectorModal(chargePointIndex: number, connectorIndex: number) {
    const connector = this.chargePoints[chargePointIndex].connectors[connectorIndex];
    this.connectorForm.patchValue({
      speed: connector.speed,
      pricePerKWh: connector.pricePerKWh,
      pricePerMinute: connector.pricePerMinute,
      pricePerHour: connector.pricePerHour
    });
    this.currentChargePointIndex = chargePointIndex;
    this.currentConnectorIndex = connectorIndex;
    this.showConnectorModal = true;
  }
  
  closeConnectorModal() {
    this.showConnectorModal = false;
  }
  
  saveConnector() {
    if (this.connectorForm.invalid) {
      this.markFormGroupTouched(this.connectorForm);
      return;
    }
    
    if (this.currentChargePointIndex === null) {
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
    };
    
    const chargePoint = this.chargePoints[this.currentChargePointIndex];
    
    // Initialize connectors array if it doesn't exist
    if (!chargePoint.connectors) {
      chargePoint.connectors = [];
    }
    
    // Check if we're adding a new connector or editing an existing one
    if (this.currentConnectorIndex !== null) {
      chargePoint.connectors[this.currentConnectorIndex] = connector;
    } else {
      // Make sure we don't exceed connector limit based on category
      const maxConnectors = chargePoint.category === ChargePointCategoryEnum.Single ? 1 : 2;
      if (chargePoint.connectors.length >= maxConnectors) {
        alert(`A ${chargePoint.category} charge point can only have ${maxConnectors} connector(s).`);
        return;
      }
      chargePoint.connectors.push(connector);
    }
    
    this.closeConnectorModal();
  }
  
  deleteChargePoint(index: number) {
    if (confirm('Are you sure you want to delete this charge point?')) {
      this.chargePoints.splice(index, 1);
    }
  }
  
  deleteConnector(chargePointIndex: number, connectorIndex: number) {
    if (confirm('Are you sure you want to delete this connector?')) {
      this.chargePoints[chargePointIndex].connectors.splice(connectorIndex, 1);
    }
  }
  
  getChargePointCategoryOptions() {
    return [
      { value: ChargePointCategoryEnum.Single, label: 'Single' },
      { value: ChargePointCategoryEnum.Double, label: 'Double' }
    ];
  }
  
  getChargePointStatusOptions() {
    return Object.values(ChargePointStatusEnum).map(value => ({
      value: value,
      label: value
    }));
  }
  
  canAddConnector(chargePoint: any): boolean {
    const maxConnectors = chargePoint.category === ChargePointCategoryEnum.Single ? 1 : 2;
    return chargePoint.connectors.length < maxConnectors;
  }
  
  // Helper method to mark all controls in a form group as touched
  markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
  
  getErrorMessage(control: AbstractControl | null): string {
    if (!control) return '';
    
    if (control.hasError('required')) {
      return 'This field is required';
    }
    return '';
  }
  
  isInvalid(control: FormControl): boolean {
    return control.invalid && control.touched;
  }

  validateChargePoints(): { valid: boolean, message: string } {
    // Check if there's at least one charge point
    if (this.chargePoints.length === 0) {
      return { valid: false, message: 'You must add at least one charge point.' };
    }
    
    // Validate that each charge point has the correct number of connectors
    for (let i = 0; i < this.chargePoints.length; i++) {
      const chargePoint = this.chargePoints[i];
      
      // Check if connectors array exists
      if (!chargePoint.connectors || !Array.isArray(chargePoint.connectors)) {
        return { 
          valid: false, 
          message: `Charge point #${i + 1} with serial number ${chargePoint.serialNumber} has no connectors.` 
        };
      }

      console.log(chargePoint);
      
      // For Single type, must have exactly 1 connector
      console.log(chargePoint.category)
      if (chargePoint.category == "Single" && chargePoint.connectors.length != 1) {
        console.log("this is in here");
        return { 
          valid: false, 
          message: `Charge point #${i + 1} with serial number ${chargePoint.serialNumber} is of type Single and must have exactly 1 connector.` 
        };
      }
      
      // For Double type, must have exactly 2 connectors
      if (chargePoint.category === "Double" && chargePoint.connectors.length !== 2) {
        return { 
          valid: false, 
          message: `Charge point #${i + 1} with serial number ${chargePoint.serialNumber} is of type Double and must have exactly 2 connectors.` 
        };
      }
    }
    
    return { valid: true, message: '' };
  }

  saveInformations() {
    const validation = this.validateChargePoints();
    
    if (!validation.valid) {
      // Show error message to the user
      alert(validation.message);
      return;
    }
    
    // If validation passes, proceed with emitting event
    this.nextStep.emit(this.chargePoints);
  }


  previousStep = () => this.previousStepEvent.emit();

}
