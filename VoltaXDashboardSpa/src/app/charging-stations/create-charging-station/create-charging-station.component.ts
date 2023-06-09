import { Component, OnInit } from '@angular/core';
import { FormArray, FormGroup, FormControl } from '@angular/forms';
@Component({
  selector: 'app-create-charging-station',
  templateUrl: './create-charging-station.component.html',
  styleUrls: ['./create-charging-station.component.css']
})
export class CreateChargingStationComponent implements OnInit {

  form: FormGroup;

  constructor() {
    this.form = new FormGroup({
      chargingStationName : new FormControl(''),
      chargingStationNetwork : new FormControl(''),
      chargingStationCategory : new FormControl(''),
      chargingStationChargerQuantity : new FormControl(''),
      chargingStationAddress : new FormControl(''),
      chargingStationCountry : new FormControl(''),
      chargingStationState : new FormControl(''),
      chargingStationCity : new FormControl(''),
      chargingStationZipCode : new FormControl(''),
      chargingStationLatitude : new FormControl(''),
      chargingStationLongitude : new FormControl(''),
      chargingStationOrganisation : new FormControl(''),
      chargingStationPublish : new FormControl(''),
      chargingStationParkingType : new FormControl(''),
      chargingStationStatus : new FormControl(''),
      chargingStationAmenities : new FormGroup({
        wifi: new FormControl(false),
        parking: new FormControl(false),
        restaurants: new FormControl(false),
        washroom: new FormControl(false),
        sittingArea: new FormControl(false)
      }),
      chargePoints: new FormArray([
        new FormGroup({
          chargePointName: new FormControl(''),
          chargePointId: new FormControl(''),
          chargePointSerialNumber: new FormControl(''),
          chargePointMake: new FormControl(''),
          chargePointStatus: new FormControl(''),
        })
      ])
    });
  }

  get chargePoints() {
    return this.form.get('chargePoints') as FormArray;
  }

  addChargePoint() {
    const chargePoints = this.form.get('chargePoints') as FormArray;

    if (chargePoints.length < 5) {
      chargePoints.push(new FormGroup({
        chargePointName: new FormControl(''),
        chargePointId: new FormControl(''),
        chargePointSerialNumber: new FormControl(''),
        chargePointMake: new FormControl(''),
        chargePointStatus: new FormControl(''),
        // other form controls...
      }));
    } else {
      alert('Maximum 5 charge points are allowed');
    }
  }

  ngOnInit() {
  }

  onSubmit() {
    console.log(this.form.value);
  }

}
