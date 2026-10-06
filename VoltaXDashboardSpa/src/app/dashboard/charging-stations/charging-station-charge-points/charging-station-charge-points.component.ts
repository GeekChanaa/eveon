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
  loading = true;
  loadError = false;
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
    this.loading = true;
    this.loadError = false;
    this._chargePointService.getChargingStationChargePoints(this.chargingStationID).subscribe({
      next: data => { this.chargePoints = data; this.loading = false; },
      error: () => { this.loadError = true; this.loading = false; }
    });
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
