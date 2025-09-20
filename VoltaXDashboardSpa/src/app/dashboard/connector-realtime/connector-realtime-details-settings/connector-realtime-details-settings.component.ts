import { Component, Input, OnInit } from '@angular/core';
import { ActionModalStatusEnum } from 'src/_models/_enums/action-modal-status-enum';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-connector-realtime-details-settings',
  templateUrl: './connector-realtime-details-settings.component.html',
  styleUrls: ['./connector-realtime-details-settings.component.sass']
})
export class ConnectorRealtimeDetailsSettingsComponent implements OnInit {

  @Input() chargePoint : any = {};

  constructor(
    private _chargePointService : ChargePointService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
  }

  showChargeOnMapToggle(val : boolean){
    this._chargePointService.setShowOnMap(this.chargePoint.id, val).subscribe((data) => {
      this.chargePoint.showOnMap = val;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Show Charge On Map Changed Successfully",4000);
    },(error)=>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }

  hasChargeCableToggle(val : boolean){
    this._chargePointService.setHasChargeCable(this.chargePoint.id, val).subscribe((data) => {
      this.chargePoint.hasChargeCable = val;
      this._modalService.popup(ActionModalStatusEnum.Success,"Succcess !","Has Charge Cable Changed Successfully",4000);
    },(error)=>{
      this._modalService.popup(ActionModalStatusEnum.Error,"Error","Something went wrong",4000);
    })
  }

}
