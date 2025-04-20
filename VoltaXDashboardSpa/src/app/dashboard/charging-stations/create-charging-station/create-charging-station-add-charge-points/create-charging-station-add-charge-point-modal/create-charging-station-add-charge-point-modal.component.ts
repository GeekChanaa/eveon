import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-create-charging-station-add-charge-point-modal',
  templateUrl: './create-charging-station-add-charge-point-modal.component.html',
  styleUrls: ['./create-charging-station-add-charge-point-modal.component.sass']
})
export class CreateChargingStationAddChargePointModalComponent implements OnInit {

  @Input() form : FormGroup = new FormGroup({});

  constructor() { }

  ngOnInit() {
    console.log("this is the chargepoint modal value");
    console.log(this.form.value);
  }

  getControl(name: string): FormControl {
    return this.form.get(name) as FormControl;
  }

}
