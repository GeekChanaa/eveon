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
  cpfShow : Boolean = false;
  isChargePointVisible : boolean = false;
  displayedChargePointID : number = 0;

  constructor(
    private _chargePointService: ChargePointService
  ) { 
    
  }

  ngOnInit() {
    this.getChargingStationChargePoints();
  }

  
  getChargingStationChargePoints(){
    this._chargePointService.getChargingStationChargePoints(this.chargingStationID).subscribe((data) => {
      this.chargePoints = data;
      this.cpfShow = false;
    })
  }
  deleteChargePoint(id : number){
    this._chargePointService.deleteById(id).subscribe((data) => {
      this.getChargingStationChargePoints();
    });
  }

  refresh(){
    this.getChargingStationChargePoints();
    this.isChargePointVisible = false;  
  }

  showChargePoint(id : number){
    this.isChargePointVisible = true;
    this.displayedChargePointID = id;
  }



}
