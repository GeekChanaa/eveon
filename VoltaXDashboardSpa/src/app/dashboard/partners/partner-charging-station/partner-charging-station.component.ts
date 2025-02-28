import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActionModalService } from 'src/_services/action-modal.service';
import { ChargingStationService } from 'src/_services/charging-station.service';
import { EnumMappingService } from 'src/_services/enum-mapping.service';

@Component({
  selector: 'app-partner-charging-station',
  templateUrl: './partner-charging-station.component.html',
  styleUrls: ['./partner-charging-station.component.sass']
})
export class PartnerChargingStationComponent implements OnInit {

  @Input() chargingStationID : number = 0

  @Output() refreshEvent : EventEmitter<number> = new EventEmitter<number>();

  chargePointCategory : string = "";
  chargePointStatus : string = "";

  @Input() chargingStation : any = {}

  constructor(
    private _chargingStationService : ChargingStationService,
    private _enumMappings : EnumMappingService,
    private _modalService : ActionModalService
  ) { }

  ngOnInit() {
    this.getChargingStationByID();
  }

  getChargingStationByID(){
    this._chargingStationService.getChargingStationByID(this.chargingStationID).subscribe((data) => {
      console.log("this is the charging station");
      console.log(data);
      this.chargingStation = data;
    })
  }


}
