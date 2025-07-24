import { Component, Input, OnInit } from '@angular/core';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-connector-realtime-details-settings',
  templateUrl: './connector-realtime-details-settings.component.html',
  styleUrls: ['./connector-realtime-details-settings.component.sass']
})
export class ConnectorRealtimeDetailsSettingsComponent implements OnInit {

  @Input() chargePoint : any = {};

  constructor(
    private _chargePointService : ChargePointService
  ) { }

  ngOnInit() {
  }

  showChargeOnMapToggle(val : boolean){
    this._chargePointService.setShowOnMap(this.chargePoint.id, val).subscribe((data) => {
      this.chargePoint.showOnMap = val;
    })
  }

  hasChargeCableToggle(val : boolean){
    this._chargePointService.setHasChargeCable(this.chargePoint.id, val).subscribe((data) => {
      this.chargePoint.hasChargeCable = val;
    })
  }

}
