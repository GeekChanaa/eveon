import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charging-station-charge-points',
  templateUrl: './charging-station-charge-points.component.html',
  styleUrls: ['./charging-station-charge-points.component.sass']
})
export class ChargingStationChargePointsComponent implements OnInit {

  @Input() chargingStationID : number = 0;
  chargePoints : any[] = [];
  chargePointForm : FormGroup;
  cpfShow : Boolean = false;

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
    this.getChargingStationChargePoints();
  }

  
  getChargingStationChargePoints(){
    this._chargePointService.getChargingStationChargePoints(this.chargingStationID).subscribe((data) => {
      this.chargePoints = data;
    })
  }


  
  // Charge Point Form
  showChargePointForm(){
    this.cpfShow = true;
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
      this.getChargingStationChargePoints();
    })
  }

  getControl(name: string): FormControl {
    return this.chargePointForm.get(name) as FormControl;
  }

  deleteChargePoint(id : number){
    this._chargePointService.deleteById(id).subscribe((data) => {
      this.getChargingStationChargePoints();
    });
  }



}
