import { AfterViewInit, Component, OnInit } from '@angular/core';
import { FormArray, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { AuthService } from 'src/_services/auth.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';
import { UserService } from 'src/_services/user.service';

declare var $: any;

enum CreateChargingStationFormStepEnum{
  ChargingStationTypeChoice = 0,
  ChargingStationInformations = 1,
  ChargingStationAddress = 2,
  AddChargePoints = 3,
  Images = 4,
  Preview = 5
}

export enum ChargingStationTypeEnum{
  VoltaXStation = 0,
  PartnerStation = 1
}

@Component({
  selector: 'app-create-charging-station',
  templateUrl: './create-charging-station.component.html',
  styleUrls: ['./create-charging-station.component.sass']
})
export class CreateChargingStationComponent implements OnInit, AfterViewInit  {

  chargingStationInformationsForm : FormGroup;
  chargingStationAddressForm : FormGroup;
  chargingStationImages : any = {};

  isLoading : boolean = false;

  CreateChargingStationFormStepEnum = CreateChargingStationFormStepEnum;
  chargingStationType : ChargingStationTypeEnum = ChargingStationTypeEnum.VoltaXStation;

  currentStep = CreateChargingStationFormStepEnum.ChargingStationTypeChoice;

  chargePoints : any[] = [];

  ngAfterViewInit() {
  }

  nextStep(step : CreateChargingStationFormStepEnum){
    this.currentStep = step;
  }

  constructor(
    private _modalService:  ActionModalService,
    private _chargingStationService:  ChargingStationService,
    private _router : Router
  ) {
    this.chargingStationInformationsForm = new FormGroup({
      category: new FormControl('Public',[Validators.required]),
      network: new FormControl('Public',[Validators.required]),
      partnerID: new FormControl(''),
      chargerQuantity: new FormControl('1',[Validators.required]),
      parkingType: new FormControl('ParallelParking',[Validators.required]),
      status: new FormControl('Available',[Validators.required]),
      wifi: new FormControl(false),
      parking: new FormControl(false),
      restaurants: new FormControl(false),
      washroom: new FormControl(false),
      sittingArea: new FormControl(false)
    });

    if (this.chargingStationType === ChargingStationTypeEnum.PartnerStation) {
      this.chargingStationInformationsForm.get('partnerID')?.setValidators([Validators.required]);
    }

    this.chargingStationAddressForm = new FormGroup({
      address: new FormControl('', [Validators.required]),
      city: new FormControl('', [Validators.required]),
      zipCode: new FormControl('', [Validators.pattern(/^\d{5}$/)])
    });
    
  }

  ngOnInit() {
  }

  chargingStationTypeSelected(choice : ChargingStationTypeEnum){
    this.chargingStationType = choice;
    this.currentStep  = CreateChargingStationFormStepEnum.ChargingStationInformations;
  }

  savingChargePoints(chargePoints : any[]){
    this.chargePoints = chargePoints;
    this.currentStep = CreateChargingStationFormStepEnum.Images;
  }

  savingImages(items : any){
    this.chargingStationImages = items;
    this.currentStep = CreateChargingStationFormStepEnum.Preview;
  }

  submitForm(){
    this.isLoading = true;
    // Create FormData object
    const formData = new FormData();

    // Add charging station information
    formData.append('category', this.chargingStationInformationsForm.get('category')?.value);
    formData.append('network', this.chargingStationInformationsForm.get('network')?.value);
    formData.append('partnerID', this.chargingStationInformationsForm.get('partnerID')?.value);
    formData.append('chargerQuantity', this.chargingStationInformationsForm.get('chargerQuantity')?.value);
    formData.append('parkingType', this.chargingStationInformationsForm.get('parkingType')?.value);
    formData.append('status', this.chargingStationInformationsForm.get('status')?.value);
    
    // Add amenities
    formData.append('wifiAmenity', this.chargingStationInformationsForm.get('wifi')?.value);
    formData.append('parkingAmenity', this.chargingStationInformationsForm.get('parking')?.value);
    formData.append('restaurantsAmenity', this.chargingStationInformationsForm.get('restaurants')?.value);
    formData.append('washroomAmenity', this.chargingStationInformationsForm.get('washroom')?.value);
    formData.append('sittingAreaAmenity', this.chargingStationInformationsForm.get('sittingArea')?.value);

    // Add address information
    formData.append('address', this.chargingStationAddressForm.get('address')?.value);
    formData.append('city', this.chargingStationAddressForm.get('city')?.value);
    if (this.chargingStationAddressForm.get('zipCode')?.value) {
      formData.append('zipCode', this.chargingStationAddressForm.get('zipCode')?.value);
    }
    
    // Add default values for properties not in forms but required by the DTO
    formData.append('network', 'MainNetwork'); // Default network or get from another form
    formData.append('country', 'DefaultCountry'); // You might want to add this to your address form
    
    // Add charge points
    if (this.chargePoints && this.chargePoints.length > 0) {
      // Append each chargePoint individually
      this.chargePoints.forEach((chargePoint, index) => {
        // Append the chargePoint's basic properties
        formData.append(`chargePoints[${index}][serialNumber]`, chargePoint.serialNumber);
        formData.append(`chargePoints[${index}][status]`, chargePoint.status);
        formData.append(`chargePoints[${index}][category]`, chargePoint.category);
        
        // If the chargePoint has connectors, append each one
        if (chargePoint.connectors && chargePoint.connectors.length > 0) {
          chargePoint.connectors.forEach((connector : any, connIdx : number) => {
            formData.append(`chargePoints[${index}][connectors][${connIdx}][power]`, connector.power);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][pricePerKWh]`, connector.pricePerKWh);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][pricePerIdleMinute]`, connector.pricePerIdleMinute);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][pricePerMinute]`, connector.pricePerMinute);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][costPerKwh]`, connector.costPerKwh);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][flatFee]`, connector.flatFee);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][type]`, connector.type);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][connectorID]`, connector.connectorID);
            formData.append(`chargePoints[${index}][connectors][${connIdx}][evseID]`, connector.evseID);
          });
        }
      });
    }

    // Add images - this is handled specifically because FormData needs special handling for files
    if (this.chargingStationImages && this.chargingStationImages.length > 0) {
      for (let i = 0; i < this.chargingStationImages.length; i++) {
        // Use the naming convention expected by your backend (chargingStationImages[0], chargingStationImages[1], etc.)
        formData.append(`chargingStationImages[${i}]`, this.chargingStationImages[i]);
      }
    }

    // Call the API service
    this._chargingStationService.createChargingStation(formData).subscribe((data) => {
      this.isLoading = true;
      this._modalService.popup(ActionModalStatusEnum.Success,"Charging Station Created","The charging station has been created successfully!",4000);
      this._router.navigateByUrl("/dashboard/charging-stations");
    },(error) => {
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong, please try again later!",4000);
    })
  }

  
}
