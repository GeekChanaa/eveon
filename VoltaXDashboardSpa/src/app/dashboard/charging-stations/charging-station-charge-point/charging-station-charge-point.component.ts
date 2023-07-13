import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ChargePointCategoryEnum } from 'src/_models/_enums/charge-point-category';
import { ChargePointStatusEnum } from 'src/_models/_enums/charge-point-status';
import { ChargePoint } from 'src/_models/charge-point';
import { ChargingStation } from 'src/_models/charging-station';
import { ChargePointService } from 'src/_services/charge-point.service';

@Component({
  selector: 'app-charging-station-charge-point',
  templateUrl: './charging-station-charge-point.component.html',
  styleUrls: ['./charging-station-charge-point.component.css']
})
export class ChargingStationChargePointComponent implements OnInit {

  @Output() deleteEvent : EventEmitter<number> = new EventEmitter<number>();

  @Input() chargePoint : ChargePoint = {
    id: 0,
    chargePointId: '',
    chargingStationID: 0,
    name: '',
    serialNumber: '',
    make: '',
    status: ChargePointStatusEnum.Available,
    comment: '',
    username: '',
    password: '',
    clientCertThumb: '',
    connectors: [],
    transactions: [],
    category: ChargePointCategoryEnum.TheTower
  }

  constructor(
    private _chargePointService : ChargePointService
  ) { }

  ngOnInit() {
  }

  deleteChargePoint(id : number){
    this.deleteEvent.emit(id);
  }

}
