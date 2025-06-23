import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';
import { ChargingStationService } from 'src/_services/charging-station.service';

enum CreateChargePointFormEnum{
  ChargePointInformations = 0,
  ChargePointConnectors = 1,
  Preview = 2
}

@Component({
  selector: 'app-create-charge-point',
  templateUrl: './create-charge-point.component.html',
  styleUrls: ['./create-charge-point.component.sass']
})
export class CreateChargePointComponent implements OnInit {

  chargePointInformationsForm : FormGroup;
  isLoading : boolean = false;

  CreateChargePointFormEnum = CreateChargePointFormEnum;

  currentStep = CreateChargePointFormEnum.ChargePointInformations;

  constructor(
      private _fb: FormBuilder,
    ) {
      this.chargePointInformationsForm = this._fb.group({
        chargingStationID: new FormControl('', [Validators.required]),
        serialNumber: new FormControl('', [Validators.required]),
        status: new FormControl("Available"),
        category: new FormControl("Single"),
      });
    }
  
    ngOnInit() {
    }

  nextStep(step : CreateChargePointFormEnum){
    this.currentStep = step;
  }

  saveChargePoint(){
    console.log(this.chargePointInformationsForm.value);
  }
}
