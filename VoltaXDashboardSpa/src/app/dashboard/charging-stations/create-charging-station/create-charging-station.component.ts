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
  Preview = 4
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
  ) {
    this.chargingStationInformationsForm = new FormGroup({
      category: new FormControl('Public',[Validators.required]),
      partnerID: new FormControl('',[Validators.required]),
      chargerQuantity: new FormControl('1',[Validators.required]),
      parkingType: new FormControl('ParallelParking',[Validators.required]),
      status: new FormControl('Available',[Validators.required]),
      wifi: new FormControl(false),
      parking: new FormControl(false),
      restaurants: new FormControl(false),
      washroom: new FormControl(false),
      sittingArea: new FormControl(false)
    });

    this.chargingStationAddressForm = new FormGroup({
      address: new FormControl('', [Validators.required]),
      city: new FormControl('', [Validators.required]),
      zipCode: new FormControl(''),
    });
    
  }



  ngOnInit() {
  }

  chargingStationTypeSelected(choice : ChargingStationTypeEnum){
    this.chargingStationType = choice;
    this.currentStep  = CreateChargingStationFormStepEnum.ChargingStationInformations;
  }

  savingChargePoints(chargePoints : any[]){
    console.log("saving charge points");
    console.log(chargePoints)
    this.chargePoints = chargePoints;
    this.currentStep = CreateChargingStationFormStepEnum.Preview;
  }

  
}
