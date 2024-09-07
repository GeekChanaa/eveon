import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charging-station-add-charge-point',
  templateUrl: './charging-station-add-charge-point.component.html',
  styleUrls: ['./charging-station-add-charge-point.component.sass']
})
export class ChargingStationAddChargePointComponent implements OnInit {

  chargePointForm : FormGroup;
  @Input() chargingStationID : number = 0;
  @Output() successEvent : EventEmitter<void> = new EventEmitter();
  @Output() cancelEvent : EventEmitter<void> = new EventEmitter();

  constructor(
    private _chargePointService: ChargePointService
  ) {
    this.chargePointForm = new FormGroup({
      serialNumber : new FormControl(''),
      make : new FormControl(''),
      status : new FormControl('Available'),
      category : new FormControl('TheTower'),
      comment : new FormControl(''),
      chargePointCategory : new FormControl(''),
    })
   }

  ngOnInit() {
  }

  cpfOnSubmit(){
    var cpf = this.chargePointForm.value;
    const chargePoint : any = {
      name: "",
      serialNumber: cpf.serialNumber,
      category : cpf.category,
      make: cpf.make,
      status: cpf.status,
      comment: cpf.comment,
      username: '',
      password: '',
      clientCertThumb: '',
      chargePointId: '',
      chargingStationID: this.chargingStationID
    }
    this._chargePointService.create(chargePoint).subscribe((data) => {
      this.successEvent.emit();
    })
  }

  getControl(name: string): FormControl {
    return this.chargePointForm.get(name) as FormControl;
  }
}
